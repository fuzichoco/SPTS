// Trang quản lý tài khoản bằng AJAX:
//  - XMLHttpRequest: tải bảng dữ liệu và chuyển trang (Skip/Take ở server)
//  - fetch():        tìm kiếm
//  - jQuery AJAX:    thêm (Create) và khóa (Delete) tài khoản
(function () {
    const app = document.getElementById('usersApp');
    const listUrl = app.dataset.listUrl;
    const deleteUrl = app.dataset.deleteUrl;
    const rows = document.getElementById('userRows');
    const state = { keyword: '', page: 1, pageSize: 10 };

    const queryString = () => new URLSearchParams(state).toString();

    // 1 + 3. XMLHttpRequest: tải một trang dữ liệu
    function loadPage(page) {
        state.page = page;
        const xhr = new XMLHttpRequest();
        xhr.open('GET', `${listUrl}?${queryString()}`);
        xhr.responseType = 'json';
        xhr.onload = () => xhr.status === 200 ? render(xhr.response) : showAlert('danger', 'Không tải được dữ liệu.');
        xhr.onerror = () => showAlert('danger', 'Lỗi kết nối máy chủ.');
        xhr.send();
    }

    // 4. fetch(): tìm kiếm, luôn quay về trang 1
    async function search() {
        state.keyword = document.getElementById('keyword').value.trim();
        state.page = 1;
        try {
            const res = await fetch(`${listUrl}?${queryString()}`, { headers: { Accept: 'application/json' } });
            if (!res.ok) throw new Error(res.status);
            render(await res.json());
        } catch {
            showAlert('danger', 'Tìm kiếm thất bại.');
        }
    }

    // Cập nhật DOM từ JSON; dùng textContent để không chèn HTML từ dữ liệu người dùng
    function render(data) {
        rows.replaceChildren(...data.items.map(rowFor));
        if (data.items.length === 0)
            rows.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Không có dữ liệu</td></tr>';
        document.getElementById('summary').textContent = `Tổng: ${data.totalItems} tài khoản`;
        renderPager(data.page, data.totalPages);
    }

    function cell(text) {
        const td = document.createElement('td');
        td.textContent = text ?? '';
        return td;
    }

    function rowFor(u) {
        const tr = document.createElement('tr');
        tr.append(cell(u.username), cell(u.fullName), cell(u.email), cell(u.roles.join(', ')));

        const badge = document.createElement('span');
        badge.className = 'badge ' + (u.isActive ? 'bg-success' : 'bg-secondary');
        badge.textContent = u.isActive ? 'Hoạt động' : 'Đã khóa';
        const status = cell();
        status.append(badge);

        const actions = cell();
        if (u.isActive) {
            const btn = document.createElement('button');
            btn.className = 'btn btn-sm btn-outline-danger';
            btn.textContent = 'Khóa';
            btn.addEventListener('click', () => lockUser(u.userId, u.username));
            actions.append(btn);
        }
        tr.append(status, actions);
        return tr;
    }

    // ponytail: hiện mọi số trang, đổi sang dạng cửa sổ (trang ±2) nếu số trang lớn
    function renderPager(page, totalPages) {
        const pager = document.getElementById('pager');
        pager.replaceChildren();
        for (let p = 1; p <= totalPages; p++) {
            const li = document.createElement('li');
            li.className = 'page-item' + (p === page ? ' active' : '');
            const a = document.createElement('a');
            a.className = 'page-link';
            a.href = '#';
            a.textContent = p;
            a.addEventListener('click', e => { e.preventDefault(); loadPage(p); });
            li.append(a);
            pager.append(li);
        }
    }

    const token = () => $('#createForm input[name="__RequestVerificationToken"]').val();

    // 2. jQuery AJAX: thêm tài khoản
    $('#createForm').on('submit', function (e) {
        e.preventDefault();
        const form = this;
        $.ajax({ url: $(form).attr('action'), type: 'POST', data: $(form).serialize() })
            .done(res => {
                showAlert('success', res.message);
                form.reset();
                $('#createErrors').empty();
                loadPage(1);
            })
            .fail(xhr => {
                const errors = xhr.responseJSON?.errors ?? ['Thêm tài khoản thất bại.'];
                $('#createErrors').empty().append(errors.map(m => $('<div>').text(m)));
            });
    });

    // 2. jQuery AJAX: khóa tài khoản (xóa mềm)
    function lockUser(id, username) {
        if (!confirm(`Khóa tài khoản ${username}?`)) return;
        $.ajax({ url: `${deleteUrl}/${id}`, type: 'POST', data: { __RequestVerificationToken: token() } })
            .done(res => { showAlert('success', res.message); loadPage(state.page); })
            .fail(xhr => showAlert('danger', xhr.responseJSON?.message ?? 'Khóa tài khoản thất bại.'));
    }

    function showAlert(type, message) {
        const div = document.createElement('div');
        div.className = `alert alert-${type}`;
        div.textContent = message;
        document.getElementById('alertBox').replaceChildren(div);
    }

    let timer;
    document.getElementById('keyword').addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(search, 300);
    });
    document.getElementById('pageSize').addEventListener('change', e => {
        state.pageSize = Number(e.target.value);
        loadPage(1);
    });

    loadPage(1);
})();
