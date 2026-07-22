/**
 * site.js — Global utilities dùng chung toàn bộ ứng dụng
 */

// Expose API base URL cho các JS module khác đọc
// Giá trị này sẽ được override bởi meta tag nếu layout inject vào
window.apiBaseUrl = window.apiBaseUrl || 'https://localhost:7093';

$(document).ready(function () {
    // Highlight nav link active theo URL hiện tại
    const currentPath = window.location.pathname.toLowerCase();
    $('.navbar-nav .nav-link').each(function () {
        const href = $(this).attr('href')?.toLowerCase();
        if (href && href !== '/' && currentPath.startsWith(href)) {
            $(this).addClass('active');
        }
    });

    // Tự động ẩn alert sau 5 giây
    setTimeout(function () {
        $('.alert.alert-danger, .alert.alert-success').fadeOut('slow');
    }, 5000);
});
