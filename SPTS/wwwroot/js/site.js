// Dùng chung toàn site: menu trên mobile, xác nhận xóa, thông báo
(function () {
    const sidebar = document.getElementById('sidebar');
    document.querySelectorAll('[data-sidebar-toggle]').forEach(b => b.addEventListener('click', () => sidebar?.classList.toggle('open')));
    document.querySelectorAll('[data-sidebar-close]').forEach(b => b.addEventListener('click', () => sidebar?.classList.remove('open')));

    // Form có data-confirm: hỏi lại trước khi gửi (xóa, khóa...)
    document.addEventListener('submit', e => {
        const msg = e.target.dataset?.confirm;
        if (msg && !confirm(msg)) e.preventDefault();
    });

    // Nút con mắt: hiện/ẩn mật khẩu cho mọi ô type="password"
    document.querySelectorAll('input[type="password"]').forEach(input => {
        const wrap = document.createElement('div');
        wrap.className = 'pw-wrap';
        input.before(wrap);
        wrap.append(input);
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'pw-toggle';
        const render = () => {
            const shown = input.type === 'text';
            btn.innerHTML = `<i class="bi bi-eye${shown ? '-slash' : ''}"></i>`;
            btn.setAttribute('aria-label', shown ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
            btn.title = btn.getAttribute('aria-label');
        };
        btn.addEventListener('click', () => {
            input.type = input.type === 'password' ? 'text' : 'password';
            render();
            input.focus();
        });
        render();
        wrap.append(btn);
    });

    // Tự ẩn thông báo thành công sau 4 giây
    setTimeout(() => document.querySelectorAll('.flash.alert-success').forEach(a => bootstrap.Alert.getOrCreateInstance(a).close()), 4000);
})();

// Token AntiForgery cho các lời gọi fetch POST
window.antiForgeryToken = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
