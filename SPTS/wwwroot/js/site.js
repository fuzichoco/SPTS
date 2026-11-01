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

    // Tự ẩn thông báo thành công sau 4 giây
    setTimeout(() => document.querySelectorAll('.flash.alert-success').forEach(a => bootstrap.Alert.getOrCreateInstance(a).close()), 4000);
})();

// Token AntiForgery cho các lời gọi fetch POST
window.antiForgeryToken = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
