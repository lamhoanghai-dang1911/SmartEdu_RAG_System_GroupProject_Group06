// SmartEdu global UI helpers
(function () {
    'use strict';

    // Bootstrap appends modal backdrops directly to <body>. Our page content uses
    // transform-based entrance animations, which create a separate stacking
    // context; a modal left inside that context can therefore appear underneath
    // its own backdrop. Keep every Bootstrap modal at the same DOM level as its
    // backdrop so it remains clickable and above tables/sticky page elements.
    function portalModalsToBody(root) {
        var scope = root || document;
        var modals = [];

        if (scope.nodeType === 1 && scope.matches('.modal')) {
            modals.push(scope);
        }

        if (scope.querySelectorAll) {
            modals = modals.concat(Array.from(scope.querySelectorAll('.modal')));
        }

        modals.forEach(function (modal) {
            if (modal.parentElement !== document.body) {
                document.body.appendChild(modal);
            }
        });
    }

    portalModalsToBody(document);

    // Also support modal markup added later by partial views/AJAX.
    var modalObserver = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === 1) portalModalsToBody(node);
            });
        });
    });
    modalObserver.observe(document.body, { childList: true, subtree: true });

    document.addEventListener('hidden.bs.modal', function () {
        // Only reset the page when no other modal is still open. This also clears
        // stale backdrops left behind by interrupted or repeated modal actions.
        if (!document.querySelector('.modal.show')) {
            document.querySelectorAll('.modal-backdrop').forEach(function (el) { el.remove(); });
            document.body.classList.remove('modal-open');
            document.body.style.removeProperty('overflow');
            document.body.style.removeProperty('padding-right');
        }
    });

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
