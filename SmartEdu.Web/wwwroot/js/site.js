// SmartEdu global UI helpers
(function () {
    'use strict';

    // Confirm a potentially destructive action (e.g. delete buttons).
    window.confirmAction = function (message) {
        return window.confirm(message || 'Are you sure?');
    };

    // Auto-dismiss flash alerts after 4s.
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.alert[data-auto-dismiss="true"]').forEach(function (el) {
            setTimeout(function () {
                el.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                el.style.opacity = '0';
                el.style.transform = 'translateY(-4px)';
                setTimeout(function () { el.remove(); }, 350);
            }, 4000);
        });
    });

    // Lightweight toast helper.
    window.showToast = function (message, variant) {
        variant = variant || 'primary';
        var wrap = document.createElement('div');
        wrap.className = 'position-fixed top-0 end-0 p-3';
        wrap.style.zIndex = 1080;
        wrap.innerHTML =
            '<div class="toast show align-items-center text-bg-' + variant + ' border-0" role="alert">' +
            '  <div class="d-flex">' +
            '    <div class="toast-body">' + (message || '') + '</div>' +
            '    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(wrap);
        setTimeout(function () { wrap.remove(); }, 3500);
    };
})();