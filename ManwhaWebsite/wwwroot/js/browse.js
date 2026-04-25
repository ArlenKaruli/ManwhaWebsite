(function () {
    var searchInput = document.getElementById('browseSearch');
    var genreWrap = document.getElementById('genreWrap');
    var genrePanel = document.getElementById('genrePanel');
    var genreToggle = document.getElementById('genreToggle');
    var genreCount = document.getElementById('genreCount');
    var advancedPanel = document.getElementById('advancedPanel');
    var advancedToggle = document.getElementById('advancedToggle');
    var grid = document.getElementById('browseGrid');
    var loadingEl = document.getElementById('browseLoading');
    var emptyEl = document.getElementById('browseEmpty');
    var resultsCount = document.getElementById('resultsCount');
    var resultsNote = document.getElementById('resultsNote');
    var loadMoreWrap = document.getElementById('loadMoreWrap');
    var loadMoreBtn = document.getElementById('loadMoreBtn');

    var advMinRating = document.getElementById('advMinRating');
    var advMinChapters = document.getElementById('advMinChapters');
    var advMaxChapters = document.getElementById('advMaxChapters');
    var advPublishedAfter = document.getElementById('advPublishedAfter');
    var advPublishedBefore = document.getElementById('advPublishedBefore');

    var statusBtns = document.querySelectorAll('.browse-status-btn');
    var debounceTimer = null;
    var currentRequest = null;
    var currentPage = 1;
    var totalLoaded = 0;
    var perPage = 40;

    // ── Genre dropdown ────────────────────────────────────────
    genreToggle.addEventListener('click', function (e) {
        e.stopPropagation();
        var open = genrePanel.classList.toggle('open');
        genreToggle.classList.toggle('active', open);
    });

    document.addEventListener('click', function (e) {
        if (!genreWrap.contains(e.target)) {
            genrePanel.classList.remove('open');
            genreToggle.classList.remove('active');
        }
    });

    genrePanel.querySelectorAll('input[type=checkbox]').forEach(function (cb) {
        cb.addEventListener('change', function () {
            updateGenreCount();
            triggerFilter(0);
        });
    });

    function updateGenreCount() {
        var n = genrePanel.querySelectorAll('input:checked').length;
        genreCount.textContent = n > 0 ? n : '';
        genreCount.style.display = n > 0 ? 'inline' : 'none';
    }

    // ── Status buttons ────────────────────────────────────────
    statusBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            statusBtns.forEach(function (b) { b.classList.remove('active'); });
            this.classList.add('active');
            triggerFilter(0);
        });
    });

    // ── Advanced panel ────────────────────────────────────────
    advancedToggle.addEventListener('click', function () {
        var open = advancedPanel.classList.toggle('open');
        advancedToggle.classList.toggle('active', open);
    });

    document.getElementById('advApply').addEventListener('click', function () {
        triggerFilter(0);
    });

    document.getElementById('advReset').addEventListener('click', function () {
        searchInput.value = '';
        genrePanel.querySelectorAll('input:checked').forEach(function (cb) { cb.checked = false; });
        updateGenreCount();
        statusBtns.forEach(function (b) { b.classList.remove('active'); });
        statusBtns[0].classList.add('active');
        advMinRating.value = '';
        advMinChapters.value = '';
        advMaxChapters.value = '';
        advPublishedAfter.value = '';
        advPublishedBefore.value = '';
        triggerFilter(0);
    });

    // ── Search input ──────────────────────────────────────────
    searchInput.addEventListener('input', function () {
        triggerFilter(350);
    });

    // ── Load more button ──────────────────────────────────────
    loadMoreBtn.addEventListener('click', function () {
        fetchResults(currentPage + 1, true);
    });

    // ── Filter trigger (always resets to page 1) ──────────────
    function triggerFilter(delay) {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(function () { fetchResults(1, false); }, delay);
    }

    function getSelectedStatus() {
        var btn = document.querySelector('.browse-status-btn.active');
        return btn ? btn.dataset.status : '';
    }

    function getSelectedGenres() {
        return Array.from(genrePanel.querySelectorAll('input:checked')).map(function (cb) { return cb.value; });
    }

    function hasActiveFilters() {
        var search = searchInput.value.trim();
        var genres = getSelectedGenres();
        var status = getSelectedStatus();
        var adv = advMinRating.value || advMinChapters.value || advMaxChapters.value ||
                  advPublishedAfter.value || advPublishedBefore.value;
        return !!(search || genres.length > 0 || status || adv);
    }

    function buildParams(page) {
        var params = new URLSearchParams();
        var search = searchInput.value.trim();
        if (search) params.append('search', search);

        getSelectedGenres().forEach(function (g) { params.append('genres', g); });

        var status = getSelectedStatus();
        if (status) params.append('status', status);

        var minRating = parseFloat(advMinRating.value);
        if (!isNaN(minRating) && minRating > 0) params.append('minRating', minRating);

        var minCh = parseInt(advMinChapters.value, 10);
        if (!isNaN(minCh) && minCh > 0) params.append('minChapters', minCh);

        var maxCh = parseInt(advMaxChapters.value, 10);
        if (!isNaN(maxCh) && maxCh > 0) params.append('maxChapters', maxCh);

        var after = parseInt(advPublishedAfter.value, 10);
        if (!isNaN(after) && after > 0) params.append('publishedAfter', after);

        var before = parseInt(advPublishedBefore.value, 10);
        if (!isNaN(before) && before > 0) params.append('publishedBefore', before);

        params.append('page', page);
        return params;
    }

    // ── Fetch results ─────────────────────────────────────────
    async function fetchResults(page, append) {
        if (currentRequest) currentRequest.abort();
        currentRequest = new AbortController();
        var signal = currentRequest.signal;

        if (append) {
            loadMoreBtn.disabled = true;
            loadMoreBtn.textContent = 'Loading…';
        } else {
            setLoading(true);
            loadMoreWrap.classList.remove('visible');
        }

        try {
            var res = await fetch('/Browse/Filter?' + buildParams(page).toString(), { signal: signal });
            if (!res.ok) throw new Error('fetch failed');
            var data = await res.json();

            if (append) {
                appendCards(data);
                if (data.length === perPage) {
                    currentPage = page;
                    showLoadMore(true);
                } else {
                    showLoadMore(false);
                }
            } else {
                renderCards(data);
                currentPage = 1;
                totalLoaded = data.length;
                resultsNote.textContent = hasActiveFilters() ? 'Filtered results' : 'Showing popular manhwa';
                showLoadMore(data.length === perPage);
            }
        } catch (e) {
            if (e.name === 'AbortError') return;
            if (append) showLoadMore(true);
        } finally {
            if (!signal.aborted) {
                setLoading(false);
                loadMoreBtn.disabled = false;
                loadMoreBtn.textContent = 'Load More';
                currentRequest = null;
            }
        }
    }

    function setLoading(on) {
        grid.style.opacity = on ? '0.35' : '1';
        loadingEl.style.display = on ? 'flex' : 'none';
    }

    function showLoadMore(visible) {
        loadMoreWrap.classList.toggle('visible', visible);
    }

    // ── Render (replace) ──────────────────────────────────────
    function renderCards(items) {
        resultsCount.textContent = items.length + ' title' + (items.length === 1 ? '' : 's');

        if (items.length === 0) {
            grid.innerHTML = '';
            grid.style.display = 'none';
            emptyEl.style.display = 'flex';
            return;
        }

        grid.style.display = '';
        emptyEl.style.display = 'none';
        grid.innerHTML = items.map(cardHtml).join('');
    }

    // ── Append (load more) ────────────────────────────────────
    function appendCards(items) {
        if (!items.length) return;
        totalLoaded += items.length;
        resultsCount.textContent = totalLoaded + ' title' + (totalLoaded === 1 ? '' : 's');
        grid.insertAdjacentHTML('beforeend', items.map(cardHtml).join(''));
    }

    function cardHtml(m) {
        var score = m.score > 0
            ? '<div class="updated-chapter">★ ' + m.score.toFixed(1) + '</div>'
            : '';
        return '<a href="/manga/' + m.id + '" class="updated-card">' +
            '<div class="updated-cover"><img src="' + esc(m.cover) + '" alt="' + esc(m.title) + '" loading="lazy"></div>' +
            '<div class="updated-info">' +
            '<div class="updated-title">' + esc(m.title) + '</div>' +
            score +
            '<div class="updated-time">' + formatStatus(m.status) + '</div>' +
            '</div></a>';
    }

    function formatStatus(s) {
        if (s === 'FINISHED') return 'Completed';
        if (s === 'RELEASING') return 'Ongoing';
        if (s === 'NOT_YET_RELEASED') return 'Upcoming';
        return s || '';
    }

    function esc(str) {
        return (str || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ── Init ──────────────────────────────────────────────────
    updateGenreCount();
    var initialCount = grid.querySelectorAll('.updated-card').length;
    totalLoaded = initialCount;
    // Show load more if initial results filled a full page
    if (initialCount >= perPage) showLoadMore(true);
})();
