// State Management
let records = JSON.parse(localStorage.getItem('ir_records')) || [];
let currentPage = 1;
const itemsPerPage = 10;
let currentSort = { field: 'year', direction: 'desc' };
let searchQuery = '';

// DOM Elements
const dashboardView = document.getElementById('dashboard-view');
const recordsView = document.getElementById('records-view');
const navDashboard = document.getElementById('nav-dashboard');
const navRecords = document.getElementById('nav-records');
const recordModal = document.getElementById('record-modal');
const addRecordBtn = document.getElementById('add-record-btn');
const closeModals = document.querySelectorAll('.close-modal');
const recordForm = document.getElementById('record-form');
const dropZone = document.getElementById('drop-zone');
const fileInput = document.getElementById('file-input');
const fileInfo = document.getElementById('file-info');
const fileNameDisplay = document.getElementById('file-name');
const removeFileBtn = document.getElementById('remove-file');
const globalSearch = document.getElementById('global-search');
const recordsTbody = document.getElementById('records-tbody');
const paginationControls = document.getElementById('pagination-controls');
const exportBtn = document.getElementById('export-btn');

// State Sync
function saveRecords() {
    localStorage.setItem('ir_records', JSON.stringify(records));
    updateStats();
    renderRecords();
}

function updateStats() {
    document.getElementById('total-records').textContent = records.length;
    const uniqueInstitutions = new Set(records.map(r => r.institution)).size;
    document.getElementById('total-institutions').textContent = uniqueInstitutions;
}

// Navigation
function switchView(view) {
    if (view === 'dashboard') {
        dashboardView.classList.remove('hidden');
        recordsView.classList.add('hidden');
        navDashboard.classList.add('active');
        navRecords.classList.remove('active');
    } else {
        dashboardView.classList.add('hidden');
        recordsView.classList.remove('hidden');
        navDashboard.classList.remove('active');
        navRecords.classList.add('active');
    }
}

navDashboard.addEventListener('click', () => switchView('dashboard'));
navRecords.addEventListener('click', () => switchView('records'));

// Modal Logic
addRecordBtn.addEventListener('click', () => {
    recordModal.classList.remove('hidden');
});

closeModals.forEach(btn => {
    btn.addEventListener('click', () => {
        recordModal.classList.add('hidden');
        recordForm.reset();
        resetUploadZone();
    });
});

// File Upload Logic
let uploadedFile = null;

dropZone.addEventListener('click', () => fileInput.click());
dropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    dropZone.style.borderColor = 'var(--primary)';
});
dropZone.addEventListener('dragleave', () => {
    dropZone.style.borderColor = 'var(--glass-border)';
});
dropZone.addEventListener('drop', (e) => {
    e.preventDefault();
    handleFiles(e.dataTransfer.files);
});
fileInput.addEventListener('change', (e) => handleFiles(e.target.files));

function handleFiles(files) {
    if (files.length > 0) {
        const file = files[0];
        const reader = new FileReader();
        reader.onload = (e) => {
            uploadedFile = {
                name: file.name,
                type: file.type,
                size: file.size,
                data: e.target.result // Base64 for storage in this demo
            };
            fileNameDisplay.textContent = file.name;
            fileInfo.classList.remove('hidden');
            dropZone.classList.add('hidden');
        };
        reader.readAsDataURL(file);
    }
}

function resetUploadZone() {
    uploadedFile = null;
    fileInfo.classList.add('hidden');
    dropZone.classList.remove('hidden');
}

removeFileBtn.addEventListener('click', resetUploadZone);

// Form Submission
recordForm.addEventListener('submit', (e) => {
    e.preventDefault();
    const newRecord = {
        id: Date.now(),
        year: document.getElementById('year').value,
        type: document.getElementById('type').value,
        institution: document.getElementById('institution').value,
        value: parseFloat(document.getElementById('value').value) || 0,
        file: uploadedFile,
        createdAt: new Date().toISOString()
    };

    records.push(newRecord);
    saveRecords();
    recordModal.classList.add('hidden');
    recordForm.reset();
    resetUploadZone();
});

// Table Rendering, Sorting, Filtering
function renderRecords() {
    let filtered = records.filter(r =>
        r.institution.toLowerCase().includes(searchQuery.toLowerCase()) ||
        r.type.toLowerCase().includes(searchQuery.toLowerCase()) ||
        r.year.includes(searchQuery)
    );

    // Sorting
    filtered.sort((a, b) => {
        let valA = a[currentSort.field];
        let valB = b[currentSort.field];
        if (typeof valA === 'string') {
            return currentSort.direction === 'asc'
                ? valA.localeCompare(valB)
                : valB.localeCompare(valA);
        }
        return currentSort.direction === 'asc' ? valA - valB : valB - valA;
    });

    // Pagination
    const start = (currentPage - 1) * itemsPerPage;
    const paginated = filtered.slice(start, start + itemsPerPage);

    recordsTbody.innerHTML = paginated.map(r => `
        <tr>
            <td>${r.year}</td>
            <td><strong>${r.institution}</strong></td>
            <td><span class="badge badge-info">${r.type}</span></td>
            <td>${r.value ? r.value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) : '-'}</td>
            <td>${r.file ? `<button class="btn-text" onclick="downloadFile(${r.id})">📄 ${truncate(r.file.name, 15)}</button>` : 'N/A'}</td>
            <td>
                <button class="btn-text danger" onclick="deleteRecord(${r.id})">🗑️ Excluir</button>
            </td>
        </tr>
    `).join('');

    renderPagination(filtered.length);
}

function renderPagination(totalItems) {
    const totalPages = Math.ceil(totalItems / itemsPerPage);
    if (totalPages <= 1) {
        paginationControls.innerHTML = '';
        return;
    }

    let btns = '';
    for (let i = 1; i <= totalPages; i++) {
        btns += `<button class="btn-page ${i === currentPage ? 'active' : ''}" onclick="goToPage(${i})">${i}</button>`;
    }
    paginationControls.innerHTML = btns;
}

function goToPage(page) {
    currentPage = page;
    renderRecords();
}

function deleteRecord(id) {
    if (confirm('Deseja realmente excluir este registro?')) {
        records = records.filter(r => r.id !== id);
        saveRecords();
    }
}

function downloadFile(id) {
    const record = records.find(r => r.id === id);
    if (record && record.file) {
        const link = document.createElement('a');
        link.href = record.file.data;
        link.download = record.file.name;
        link.click();
    }
}

// Export CSV
exportBtn.addEventListener('click', () => {
    if (records.length === 0) return alert('Nenhum registro para exportar.');

    const headers = ['Ano', 'Instituicao', 'Tipo', 'Valor', 'Data Criacao'];
    const rows = records.map(r => [
        r.year,
        `"${r.institution.replace(/"/g, '""')}"`,
        r.type,
        r.value.toString().replace('.', ','),
        new Date(r.createdAt).toLocaleDateString('pt-BR')
    ]);

    let csvContent = "data:text/csv;charset=utf-8,"
        + headers.join(';') + "\n"
        + rows.map(e => e.join(";")).join("\n");

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `informes_ir_vendedor_${new Date().getFullYear()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
});

// Search
globalSearch.addEventListener('input', (e) => {
    searchQuery = e.target.value;
    currentPage = 1;
    renderRecords();
});

// Sorting Event Listeners
document.querySelectorAll('th[data-sort]').forEach(th => {
    th.addEventListener('click', () => {
        const field = th.dataset.sort;
        if (currentSort.field === field) {
            currentSort.direction = currentSort.direction === 'asc' ? 'desc' : 'asc';
        } else {
            currentSort.field = field;
            currentSort.direction = 'asc';
        }
        renderRecords();
    });
});

// Utils
function truncate(str, n) {
    return (str.length > n) ? str.substr(0, n - 1) + '...' : str;
}

// Initial Init
updateStats();
renderRecords();
