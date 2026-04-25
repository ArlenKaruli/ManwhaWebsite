(function () {
    function initSearch(inputEl, dropdownContainer) {
        if (!inputEl) return;

        var dropdown = document.createElement('div');
        dropdown.className = 'search-dropdown';
        dropdownContainer.appendChild(dropdown);

        var debounceTimer = null;
        var activeIndex = -1;
        var currentResults = [];

        function closeDropdown() {
            dropdown.classList.remove('open');
            activeIndex = -1;
        }

        function navigateToSearch(q) {
            window.location.href = '/search?q=' + encodeURIComponent(q);
        }

        function updateActive() {
            var items = dropdown.querySelectorAll('.search-dropdown-item');
            items.forEach(function (el, i) {
                el.classList.toggle('active', i === activeIndex);
            });
        }

        inputEl.addEventListener('input', function () {
            var q = this.value.trim();
            clearTimeout(debounceTimer);
            if (q.length < 2) { closeDropdown(); return; }
            debounceTimer = setTimeout(function () { fetchSuggestions(q); }, 250);
        });

        inputEl.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                var q = this.value.trim();
                if (!q) return;
                if (activeIndex >= 0 && currentResults[activeIndex]) {
                    window.location.href = '/manga/' + currentResults[activeIndex].id;
                } else {
                    navigateToSearch(q);
                }
                closeDropdown();
            } else if (e.key === 'ArrowDown') {
                e.preventDefault();
                activeIndex = Math.min(activeIndex + 1, currentResults.length - 1);
                updateActive();
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                activeIndex = Math.max(activeIndex - 1, -1);
                updateActive();
            } else if (e.key === 'Escape') {
                closeDropdown();
            }
        });

        async function fetchSuggestions(q) {
            try {
                var res = await fetch('/search/suggest?q=' + encodeURIComponent(q));
                if (!res.ok) return;
                var data = await res.json();
                currentResults = data;
                renderDropdown(data, q);
            } catch (e) {}
        }

        function renderDropdown(results, q) {
            if (!results.length) { closeDropdown(); return; }

            dropdown.innerHTML = '';
            results.forEach(function (r) {
                var a = document.createElement('a');
                a.className = 'search-dropdown-item';
                a.href = '/manga/' + r.id;
                a.innerHTML =
                    '<img src="' + (r.cover || '') + '" alt="" loading="lazy" />' +
                    '<span class="search-dropdown-item-title">' + escapeHtml(r.title) + '</span>' +
                    (r.score > 0 ? '<span class="search-dropdown-item-score">★ ' + r.score.toFixed(1) + '</span>' : '');
                dropdown.appendChild(a);
            });

            var footer = document.createElement('div');
            footer.className = 'search-dropdown-footer';
            footer.textContent = 'See all results for "' + q + '"';
            footer.addEventListener('click', function () { navigateToSearch(q); });
            dropdown.appendChild(footer);

            activeIndex = -1;
            dropdown.classList.add('open');
        }

        function escapeHtml(str) {
            return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
        }

        document.addEventListener('click', function (e) {
            if (!dropdownContainer.contains(e.target) && e.target !== inputEl) {
                closeDropdown();
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var navInput = document.getElementById('navSearchInput');
        var navContainer = document.querySelector('.nav-search');
        if (navInput && navContainer) initSearch(navInput, navContainer);

        var mobileInput = document.getElementById('mobileSearchInput');
        var mobileContainer = document.querySelector('.nav-search-overlay-inner');
        if (mobileInput && mobileContainer) initSearch(mobileInput, mobileContainer);
    });
})();
