// Planning Management JavaScript

let currentSlotId = null;
let currentSlotDate = null;

// Initialize
document.addEventListener('DOMContentLoaded', function() {
    // Set today's date as default in modals
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('slotDate')?.setAttribute('value', today);
    document.getElementById('blockDate')?.setAttribute('value', today);
});

// Modal Management
function openAddSlotModal() {
    const modal = document.getElementById('addSlotModal');
    modal.classList.add('active');
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('slotDate').value = today;
}

function openBlockSlotModal() {
    const modal = document.getElementById('blockSlotModal');
    modal.classList.add('active');
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('blockDate').value = today;
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    modal.classList.remove('active');
    // Reset forms
    if (modalId === 'addSlotModal') {
        document.getElementById('addSlotForm').reset();
    } else if (modalId === 'blockSlotModal') {
        document.getElementById('blockSlotForm').reset();
    }
}

// Close modal when clicking outside
document.addEventListener('click', function(e) {
    if (e.target.classList.contains('modal')) {
        e.target.classList.remove('active');
    }
});

// Add Available Slot
async function addAvailableSlot(event) {
    event.preventDefault();
    
    const date = document.getElementById('slotDate').value;
    const startTime = document.getElementById('slotStartTime').value;
    const endTime = document.getElementById('slotEndTime').value;
    
    if (!date || !startTime || !endTime) {
        alert('Veuillez remplir tous les champs');
        return;
    }
    
    if (startTime >= endTime) {
        alert('L\'heure de fin doit être après l\'heure de début');
        return;
    }
    
    const formData = new FormData();
    formData.append('date', date);
    formData.append('startTime', startTime);
    formData.append('endTime', endTime);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]')?.value || '');
    
    try {
        const response = await fetch('/Planning/AddAvailableSlot', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const data = await response.json();
        
        if (data.success) {
            alert(data.message);
            closeModal('addSlotModal');
            location.reload();
        } else {
            alert('Erreur: ' + data.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Une erreur est survenue');
    }
}

// Block Slot
async function blockSlot(event) {
    event.preventDefault();
    
    const date = document.getElementById('blockDate').value;
    const startTime = document.getElementById('blockStartTime').value;
    const endTime = document.getElementById('blockEndTime').value;
    const reason = document.getElementById('blockReason').value;
    
    if (!date || !startTime || !endTime) {
        alert('Veuillez remplir tous les champs');
        return;
    }
    
    if (startTime >= endTime) {
        alert('L\'heure de fin doit être après l\'heure de début');
        return;
    }
    
    const formData = new FormData();
    formData.append('date', date);
    formData.append('startTime', startTime);
    formData.append('endTime', endTime);
    formData.append('reason', reason || '');
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]')?.value || '');
    
    try {
        const response = await fetch('/Planning/BlockSlot', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const data = await response.json();
        
        if (data.success) {
            alert(data.message);
            closeModal('blockSlotModal');
            location.reload();
        } else {
            alert('Erreur: ' + data.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Une erreur est survenue');
    }
}

// Open Slot Details
async function openSlotDetails(slotId, date) {
    currentSlotId = slotId;
    currentSlotDate = date;
    
    const modal = document.getElementById('slotDetailsModal');
    const content = document.getElementById('slotDetailsContent');
    const title = document.getElementById('slotDetailsTitle');
    const deleteBtn = document.getElementById('deleteSlotBtn');
    
    // Find the slot element
    const slotElement = document.querySelector(`[data-slot-id="${slotId}"]`);
    if (!slotElement) return;
    
    const slotType = slotElement.dataset.type;
    const startTime = slotElement.dataset.start;
    const endTime = slotElement.dataset.end;
    const requestId = slotElement.dataset.requestId;
    
    title.textContent = slotType === 'Mission' ? 'Détails de la mission' : 
                       slotType === 'Blocked' ? 'Créneau bloqué' : 
                       'Créneau disponible';
    
    let html = `
        <div class="slot-details">
            <div class="detail-item">
                <strong>Date:</strong> ${new Date(date).toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
            </div>
            <div class="detail-item">
                <strong>Heure:</strong> ${startTime} - ${endTime}
            </div>
            <div class="detail-item">
                <strong>Type:</strong> ${slotType === 'Mission' ? 'Mission' : slotType === 'Blocked' ? 'Bloqué' : 'Disponible'}
            </div>
    `;
    
    const slotTitle = slotElement.querySelector('.slot-title')?.textContent;
    if (slotTitle) {
        html += `<div class="detail-item"><strong>Titre:</strong> ${slotTitle}</div>`;
    }
    
    const slotDescription = slotElement.querySelector('.slot-description')?.textContent;
    if (slotDescription) {
        html += `<div class="detail-item"><strong>Description:</strong> ${slotDescription}</div>`;
    }
    
    if (slotType === 'Mission' && requestId) {
        html += `<div class="detail-item">
            <a href="/Request/Details?id=${requestId}" class="btn-primary" style="display: inline-block; margin-top: 0.5rem;">
                <i class="bi bi-eye"></i> Voir les détails de la mission
            </a>
        </div>`;
    }
    
    html += `</div>`;
    
    content.innerHTML = html;
    
    // Show delete button only for Available and Blocked slots
    if (slotType === 'Available' || slotType === 'Blocked') {
        deleteBtn.style.display = 'block';
    } else {
        deleteBtn.style.display = 'none';
    }
    
    modal.classList.add('active');
}

// Delete Slot
async function deleteSlot() {
    if (!currentSlotId || !currentSlotDate) return;
    
    if (!confirm('Êtes-vous sûr de vouloir supprimer ce créneau ?')) {
        return;
    }
    
    const formData = new FormData();
    formData.append('date', currentSlotDate);
    formData.append('slotId', currentSlotId);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]')?.value || '');
    
    try {
        const response = await fetch('/Planning/RemoveSlot', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const data = await response.json();
        
        if (data.success) {
            alert(data.message);
            closeModal('slotDetailsModal');
            location.reload();
        } else {
            alert('Erreur: ' + data.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Une erreur est survenue');
    }
}

// Go to Today
function goToToday() {
    window.location.href = '/Planning';
}

// Sync Missions
async function syncMissions() {
    if (!confirm('Synchroniser les missions acceptées avec votre planning ?')) {
        return;
    }
    
    try {
        const response = await fetch('/Planning/SyncMissions', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const data = await response.json();
        
        if (data.success) {
            alert(data.message || 'Synchronisation réussie');
            location.reload();
        } else {
            alert('Erreur: ' + (data.message || 'Erreur lors de la synchronisation'));
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Une erreur est survenue');
    }
}

// Slot Details Styles
const style = document.createElement('style');
style.textContent = `
    .slot-details {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }
    .detail-item {
        padding: 0.75rem;
        background: #F8FAFC;
        border-radius: 8px;
        font-size: 0.9375rem;
    }
    .detail-item strong {
        color: #475569;
        margin-right: 0.5rem;
    }
`;
document.head.appendChild(style);


