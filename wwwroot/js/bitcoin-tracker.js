/*
 * Bitcoin Tracker - klientska logika pro zivy kurz, spravu dat a graf.
 * Pro ochranu proti XSS pouziva textContent API.
 */

let chart = null;
let dataTable = null;

// --- Loading overlay helpers ------------------------------------------------

function setLoading(show) {
    const overlay = document.getElementById('loading-overlay');
    if (overlay) {
        overlay.classList.toggle('active', show);
    }
}

function setButtonLoading(btn, loading) {
    if (!btn) return;
    btn.classList.toggle('btn-loading', loading);
    btn.disabled = loading;
}

function showButtonFeedback(btn, message, type) {
    const existing = btn.parentElement.querySelector('.btn-feedback');
    if (existing) existing.remove();

    const feedback = document.createElement('small');
    feedback.className = `btn-feedback ms-2 text-${type === 'error' ? 'danger' : 'success'}`;
    feedback.textContent = message;
    btn.parentElement.appendChild(feedback);

    setTimeout(() => feedback.remove(), 3000);
}

function fetchWithTimeout(ms) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), ms);
    return {
        signal: controller.signal,
        clear: () => clearTimeout(timer)
    };
}

// --- Live rate --------------------------------------------------------------

async function fetchLiveRate() {
    const liveRateEl = document.getElementById('live-rate');
    if (!liveRateEl) return;

    try {
        const timeout = fetchWithTimeout(90000);
        const response = await fetch('/api/live', { signal: timeout.signal });
        timeout.clear();

        if (!response.ok) {
            liveRateEl.textContent = 'Chyba připojení';
            return;
        }
        const data = await response.json();
        if (data.price_czk) {
            liveRateEl.textContent =
                Number(data.price_czk).toLocaleString('cs-CZ') + ' CZK';
        } else {
            liveRateEl.textContent = 'Data nedostupná';
        }
    } catch (error) {
        if (error.name === 'AbortError') {
            console.warn('Live rate fetch timed out');
            liveRateEl.textContent = 'Čas vypršel';
        } else {
            console.error('Failed to fetch live rate:', error);
            liveRateEl.textContent = 'Chyba připojení';
        }
    }
}

function scheduleLiveRate() {
    fetchLiveRate().finally(() => {
        setTimeout(scheduleLiveRate, 10000);
    });
}

// --- Saved data -------------------------------------------------------------

async function fetchSavedData() {
    const loadingIndicator = document.getElementById('saved-data-loading');
    if (loadingIndicator) loadingIndicator.style.display = 'block';

    try {
        const timeout = fetchWithTimeout(15000);
        const response = await fetch('/api/rates', { signal: timeout.signal });
        timeout.clear();

        if (!response.ok) {
            console.error('Failed to fetch saved rates:', response.statusText);
            return;
        }
        const data = await response.json();
        const tbody = document.getElementById('saved-data-body');

        if (!tbody) return;

        if (dataTable) {
            dataTable.destroy();
        }

        tbody.textContent = '';

        data.forEach(rate => {
            const row = document.createElement('tr');

            const tdDate = document.createElement('td');
            tdDate.textContent = new Date(rate.timestamp).toLocaleString();

            const tdEur = document.createElement('td');
            tdEur.textContent = Number(rate.priceEur).toFixed(2);

            const tdCzk = document.createElement('td');
            tdCzk.textContent = Number(rate.priceCzk).toFixed(2);

            const tdNote = document.createElement('td');
            const noteInput = document.createElement('input');
            noteInput.type = 'text';
            noteInput.className = 'form-control note-input';
            noteInput.dataset.id = rate.id;
            noteInput.value = rate.note || '';
            tdNote.appendChild(noteInput);

            const tdAction = document.createElement('td');
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.className = 'delete-checkbox';
            checkbox.dataset.id = rate.id;
            tdAction.appendChild(checkbox);

            row.appendChild(tdDate);
            row.appendChild(tdEur);
            row.appendChild(tdCzk);
            row.appendChild(tdNote);
            row.appendChild(tdAction);

            tbody.appendChild(row);
        });

        dataTable = $('#rates-table').DataTable({
            order: [[0, 'desc']],
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/cs.json'
            }
        });

        updateChart(data);
    } catch (error) {
        if (error.name === 'AbortError') {
            console.warn('Saved data fetch timed out');
        } else {
            console.error('Failed to fetch saved data:', error);
        }
    } finally {
        if (loadingIndicator) loadingIndicator.style.display = 'none';
    }
}

// --- Save / Update / Delete -------------------------------------------------

async function saveRate() {
    const btn = document.getElementById('save-rate');
    setButtonLoading(btn, true);

    try {
        const timeout = fetchWithTimeout(15000);
        const response = await fetch('/api/live', {
            method: 'POST',
            signal: timeout.signal
        });
        timeout.clear();

        if (response.ok) {
            showButtonFeedback(btn, 'Uloženo', 'success');
            fetchSavedData();
        } else {
            showButtonFeedback(btn, 'Chyba ukládání', 'error');
        }
    } catch (error) {
        console.error('Failed to save rate:', error);
        showButtonFeedback(btn, 'Chyba připojení', 'error');
    } finally {
        setButtonLoading(btn, false);
    }
}

async function saveChanges() {
    const btn = document.getElementById('save-changes');
    setButtonLoading(btn, true);

    const inputs = document.querySelectorAll('.note-input');
    let success = true;
    let changedCount = 0;

    for (const input of inputs) {
        const id = input.dataset.id;
        const note = input.value;
        const originalNote = input.defaultValue;

        if (note === originalNote) continue;

        if (note.trim() === '') {
            alert('Poznámka nesmí být prázdná.');
            input.focus();
            setButtonLoading(btn, false);
            return;
        }

        changedCount++;

        try {
            const timeout = fetchWithTimeout(15000);
            const response = await fetch(`/api/rates/${id}`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ note }),
                signal: timeout.signal
            });
            timeout.clear();

            if (!response.ok) {
                success = false;
            } else {
                input.defaultValue = note;
            }
        } catch (error) {
            console.error('Failed to update note for rate ' + id + ':', error);
            success = false;
        }
    }

    setButtonLoading(btn, false);

    if (changedCount === 0) {
        showButtonFeedback(btn, 'Žádné změny k uložení', 'success');
        return;
    }

    if (success) {
        showButtonFeedback(btn, `Uloženo ${changedCount} změn`, 'success');
    } else {
        showButtonFeedback(btn, 'Některé změny se nepodařilo uložit', 'error');
    }

    fetchSavedData();
}

async function deleteSelected() {
    const checkboxes = document.querySelectorAll('.delete-checkbox:checked');
    if (checkboxes.length === 0) {
        alert('Vyberte záznamy ke smazání.');
        return;
    }

    if (!confirm(`Opravdu chcete smazat ${checkboxes.length} vybraných záznamů?`)) {
        return;
    }

    const btn = document.getElementById('delete-selected');
    setButtonLoading(btn, true);

    const ids = Array.from(checkboxes).map(cb => parseInt(cb.dataset.id));
    let success = true;

    try {
        const timeout = fetchWithTimeout(15000);
        const response = await fetch('/api/rates/batch', {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids }),
            signal: timeout.signal
        });
        timeout.clear();

        if (!response.ok) {
            success = false;
            console.error('Batch delete failed:', response.statusText);
        }
    } catch (error) {
        console.error('Failed to delete rates:', error);
        success = false;
    }

    setButtonLoading(btn, false);

    if (success) {
        showButtonFeedback(btn, `Smazáno ${ids.length} záznamů`, 'success');
    } else {
        showButtonFeedback(btn, 'Chyba při mazání', 'error');
    }

    fetchSavedData();
}

// --- Chart ------------------------------------------------------------------

function updateChart(data) {
    const canvas = document.getElementById('priceChart');
    if (!canvas || !data || data.length === 0) return;

    const ctx = canvas.getContext('2d');
    const labels = data.map(r => new Date(r.timestamp).toLocaleTimeString()).reverse();
    const prices = data.map(r => r.priceCzk).reverse();

    if (chart) {
        chart.destroy();
    }

    chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'BTC/CZK',
                data: prices,
                borderColor: 'rgb(75, 192, 192)',
                tension: 0.1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false
        }
    });
}

// --- Initialization ---------------------------------------------------------

document.addEventListener('DOMContentLoaded', function () {
    const saveRateBtn = document.getElementById('save-rate');
    const saveChangesBtn = document.getElementById('save-changes');
    const deleteSelectedBtn = document.getElementById('delete-selected');

    if (saveRateBtn) saveRateBtn.addEventListener('click', saveRate);
    if (saveChangesBtn) saveChangesBtn.addEventListener('click', saveChanges);
    if (deleteSelectedBtn) deleteSelectedBtn.addEventListener('click', deleteSelected);

    fetchLiveRate();
    fetchSavedData();

    setTimeout(scheduleLiveRate, 10000);
});
