"""HTTP smoke test on a disposable seeded SPTS database. Python standard library only.
Run: python scripts/verify-catalog.py --allow-test-writes
The running application must point to a TEST database; audit logs are retained.
"""
import argparse
import html
import http.cookiejar
import os
import re
import time
import urllib.error
import urllib.parse
import urllib.request


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


class Client:
    def __init__(self, base):
        self.base = base
        self.opener = urllib.request.build_opener(
            urllib.request.ProxyHandler({}),
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect())

    def request(self, path, data=None):
        body = urllib.parse.urlencode(data).encode() if data is not None else None
        try:
            response = self.opener.open(self.base + path, body, timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.code, html.unescape(response.read().decode()), response.headers

    def get(self, path):
        status, text, _ = self.request(path)
        check(status == 200, "GET " + path)
        return text

    def post(self, path, data, form=None):
        text = self.get(form or path)
        token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', text)
        assert token, "Missing antiforgery token"
        return self.request(path, {**data, "__RequestVerificationToken": token[1]})

    def login(self, username, password):
        status, _, _ = self.post('/Account/Login', {'Username': username, 'Password': password})
        check(status == 302, 'Login ' + username)


checks = 0


def check(condition, label):
    global checks
    assert condition, label
    checks += 1
    print('PASS ' + label)


def find_id(client, controller, code):
    text = client.get('/Admin/' + controller + '?Keyword=' + code)
    match = re.search(r'/' + controller + r'/Details/(\d+)', text)
    assert match, 'Created record not found: ' + code
    return int(match[1])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-url', default='http://localhost:5223')
    parser.add_argument('--allow-test-writes', action='store_true')
    args = parser.parse_args()
    if not args.allow_test_writes:
        parser.error('Use --allow-test-writes only against a disposable seeded database.')
    client = Client(args.base_url)
    status, _, headers = client.request('/Admin/Faculties')
    check(status == 302 and '/Account/Login' in headers.get('Location', ''), 'Anonymous denied')
    client.login(os.getenv('SPTS_TEST_ADMIN', 'admin'), os.getenv('SPTS_TEST_PASSWORD', 'Admin@123'))
    faculties, majors = [], []
    prefix = 'T' + str(time.time_ns())[-6:]
    try:
        for i in range(6):
            code = prefix + str(i)
            status, _, _ = client.post('/Admin/Faculties/Create', {'FacultyCode': code, 'FacultyName': 'Test ' + code})
            check(status == 302, 'Create faculty ' + code)
            faculties.append(find_id(client, 'Faculties', code))
        faculty = faculties[0]
        status, text, _ = client.post('/Admin/Faculties/Create', {'FacultyCode': prefix+'0', 'FacultyName': 'Duplicate'})
        check(status == 200 and 'Mã khoa đã tồn tại' in text, 'Duplicate faculty rejected')
        status, text, _ = client.post('/Admin/Faculties/Create', {'FacultyCode': '', 'FacultyName': ''})
        check(status == 200 and 'Nhập mã khoa' in text, 'Required fields validated')
        text = client.get('/Admin/Faculties?' + urllib.parse.urlencode({'Keyword': prefix, 'PageSize': 5, 'Page': 2, 'SortOrder': 'name_desc'}))
        check(len(re.findall(r'/Faculties/Details/\d+', text)) == 1, 'Pagination filters before Skip/Take')
        check('SortOrder=name_desc' in text and 'Keyword='+prefix in text, 'Pagination retains filters')
        text = client.get('/Admin/Faculties?Keyword='+prefix+'&PageSize=5&Page=999')
        check('Trang 2 / 2' in text, 'Out-of-range page clamped')
        text = client.get('/Admin/Faculties?Keyword='+prefix+'&Page=-1&PageSize=0')
        check('Trang 1 / 2' in text, 'Negative page and zero size clamped')
        status, _, _ = client.post('/Admin/Faculties/Edit/'+str(faculty), {'FacultyCode': prefix+'0', 'FacultyName': 'Faculty renamed'})
        check(status == 302, 'Edit faculty')
        text = client.get('/Admin/Majors/Create')
        check('Faculty renamed' in text, 'Faculty dropdown cache invalidated')
        major_code = prefix+'M'
        status, _, _ = client.post('/Admin/Majors/Create', {'MajorCode': major_code, 'MajorName': 'Test major', 'FacultyId': faculty})
        check(status == 302, 'Create major')
        major = find_id(client, 'Majors', major_code)
        majors.append(major)
        status, text, _ = client.post('/Admin/Majors/Create', {'MajorCode': major_code, 'MajorName': 'Duplicate', 'FacultyId': faculty})
        check(status == 200 and 'Mã ngành đã tồn tại' in text, 'Duplicate major rejected')
        status, text, _ = client.post('/Admin/Majors/Create', {'MajorCode': prefix+'X', 'MajorName': 'Invalid FK', 'FacultyId': 2147483647})
        check(status == 200 and 'Khoa không tồn tại' in text, 'Invalid faculty rejected')
        status, _, _ = client.post('/Admin/Majors/Edit/'+str(major), {'MajorCode': major_code, 'MajorName': 'Major renamed', 'FacultyId': faculty})
        check(status == 302, 'Edit major')
        text = client.get('/Admin/Majors?FacultyId='+str(faculty))
        check('Major renamed' in text, 'Filter major by faculty')
        status, text, _ = client.post('/Admin/Faculties/Delete/'+str(faculty), {}, form='/Admin/Faculties/Create')
        check(status == 200 and 'Không thể xóa khoa' in text, 'Referenced faculty delete rejected server-side')
        status, _, _ = client.request('/Admin/Majors/Delete/'+str(major), {})
        check(status == 400, 'POST without CSRF token rejected')
        client.get('/Admin/Majors/Delete/'+str(major))
        check(client.request('/Admin/Majors/Details/'+str(major))[0] == 200, 'GET delete does not mutate')
        check(client.request('/Admin/Majors/Details/2147483647')[0] == 404, 'Missing record returns 404')
        student = Client(args.base_url)
        student.login('sv2310001', 'Student@123')
        for route in ['/Admin/Faculties', '/Admin/Majors']:
            status, _, headers = student.request(route)
            check(status == 302 and 'AccessDenied' in headers.get('Location', ''), 'Student denied '+route)
        logs = client.get('/Admin/AuditLogs?TableName=Faculties')
        check('CREATE' in logs and 'UPDATE' in logs, 'Audit create/update visible')
    finally:
        for controller, ids in [('Majors', majors), ('Faculties', faculties)]:
            for record in reversed(ids):
                status, _, _ = client.post('/Admin/'+controller+'/Delete/'+str(record), {})
                check(status == 302, 'Delete test '+controller+' '+str(record))
                check(client.request('/Admin/'+controller+'/Details/'+str(record))[0] == 404, 'Deleted record absent')
    print(str(checks) + ' HTTP checks passed')


if __name__ == '__main__':
    main()
