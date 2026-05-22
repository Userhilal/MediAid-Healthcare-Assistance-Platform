// ========================================
// DASHBOARD PATIENT - JAVASCRIPT
// ========================================

document.addEventListener('DOMContentLoaded', function() {
    initializeDashboard();
});

function initializeDashboard() {
    // Planning tabs
    setupPlanningTabs();
    
    // Evaluation stars
    setupEvaluationStars();
    
    // Tranquility mode
    setupTranquilityMode();
    
    // Accessibility
    setupAccessibility();
    
    // Notes
    setupPersonalNotes();
    
    // Smooth scrolling for anchor links
    setupSmoothScrolling();
    
    // Save state
    loadSavedState();
}

// Planning Tabs
function setupPlanningTabs() {
    const tabs = document.querySelectorAll('.planning-tab');
    const contents = document.querySelectorAll('.planning-tab-content');
    
    tabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Update tabs
            tabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            
            // Update contents
            contents.forEach(c => {
                c.classList.remove('active');
                if (c.id === `planning-${targetTab}`) {
                    c.classList.add('active');
                }
            });
        });
    });
}

// Evaluation Stars
function setupEvaluationStars() {
    const ratingInputs = document.querySelectorAll('.rating-stars-input');
    
    ratingInputs.forEach(ratingInput => {
        const stars = ratingInput.querySelectorAll('.star-btn');
        let selectedRating = 0;
        
        stars.forEach((star, index) => {
            star.addEventListener('click', function() {
                selectedRating = index + 1;
                updateStars(stars, selectedRating);
            });
            
            star.addEventListener('mouseenter', function() {
                highlightStars(stars, index + 1);
            });
        });
        
        ratingInput.addEventListener('mouseleave', function() {
            updateStars(stars, selectedRating);
        });
    });
}

function updateStars(stars, rating) {
    stars.forEach((star, index) => {
        const icon = star.querySelector('i');
        if (index < rating) {
            star.classList.add('active');
            icon.classList.remove('bi-star');
            icon.classList.add('bi-star-fill');
        } else {
            star.classList.remove('active');
            icon.classList.remove('bi-star-fill');
            icon.classList.add('bi-star');
        }
    });
}

function highlightStars(stars, rating) {
    stars.forEach((star, index) => {
        const icon = star.querySelector('i');
        if (index < rating) {
            icon.classList.remove('bi-star');
            icon.classList.add('bi-star-fill');
        } else {
            icon.classList.remove('bi-star-fill');
            icon.classList.add('bi-star');
        }
    });
}

// Submit Evaluation
function submitEvaluation(requestId) {
    const ratingInput = document.querySelector(`.rating-stars-input[data-request-id="${requestId}"]`);
    const stars = ratingInput.querySelectorAll('.star-btn');
    const commentTextarea = ratingInput.closest('.evaluation-card').querySelector('.evaluation-comment');
    
    let rating = 0;
    stars.forEach((star, index) => {
        if (star.classList.contains('active')) {
            rating = index + 1;
        }
    });
    
    if (rating === 0) {
        alert('Veuillez donner une note avant de soumettre.');
        return;
    }
    
    const comment = commentTextarea.value.trim();
    
    // In a real implementation, this would send to the server
    fetch('/Review/Create', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            requestId: requestId,
            rating: rating,
            comment: comment
        })
    })
    .then(response => {
        if (response.ok) {
            // Show success message
            showNotification('Merci pour votre évaluation !', 'success');
            // Remove the evaluation card
            ratingInput.closest('.evaluation-card').remove();
        } else {
            showNotification('Erreur lors de l\'envoi de l\'évaluation.', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Erreur lors de l\'envoi de l\'évaluation.', 'error');
    });
}

// Tranquility Mode
function setupTranquilityMode() {
    const notificationsPaused = document.getElementById('notificationsPaused');
    const proposalsBlocked = document.getElementById('proposalsBlocked');
    
    if (notificationsPaused) {
        notificationsPaused.addEventListener('change', function() {
            saveTranquilityState('notificationsPaused', this.checked);
            if (this.checked) {
                showNotification('Notifications suspendues', 'info');
            } else {
                showNotification('Notifications réactivées', 'success');
            }
        });
    }
    
    if (proposalsBlocked) {
        proposalsBlocked.addEventListener('change', function() {
            saveTranquilityState('proposalsBlocked', this.checked);
            if (this.checked) {
                showNotification('Nouvelles propositions bloquées', 'info');
            } else {
                showNotification('Nouvelles propositions autorisées', 'success');
            }
        });
    }
}

function saveTranquilityState(key, value) {
    localStorage.setItem(`tranquility_${key}`, value);
    // In a real implementation, this would save to the server
    fetch('/Patient/UpdateTranquilityMode', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            [key]: value
        })
    }).catch(error => console.error('Error saving tranquility state:', error));
}

// Privacy Settings

// Accessibility
function setupAccessibility() {
    // Font size
    const fontSizeButtons = document.querySelectorAll('.btn-font-size');
    fontSizeButtons.forEach(btn => {
        btn.addEventListener('click', function() {
            const size = this.getAttribute('data-size');
            changeFontSize(size);
        });
    });
    
    // High contrast
    const highContrast = document.getElementById('highContrast');
    if (highContrast) {
        highContrast.addEventListener('change', function() {
            const dashboard = document.getElementById('patientDashboard');
            if (this.checked) {
                dashboard.setAttribute('data-high-contrast', 'true');
                localStorage.setItem('accessibility_highContrast', 'true');
            } else {
                dashboard.removeAttribute('data-high-contrast');
                localStorage.removeItem('accessibility_highContrast');
            }
        });
    }
    
    // Large buttons
    const largeButtons = document.getElementById('largeButtons');
    if (largeButtons) {
        largeButtons.addEventListener('change', function() {
            const dashboard = document.getElementById('patientDashboard');
            if (this.checked) {
                dashboard.setAttribute('data-large-buttons', 'true');
                localStorage.setItem('accessibility_largeButtons', 'true');
            } else {
                dashboard.removeAttribute('data-large-buttons');
                localStorage.removeItem('accessibility_largeButtons');
            }
        });
    }
}

function changeFontSize(size) {
    const dashboard = document.getElementById('patientDashboard');
    const buttons = document.querySelectorAll('.btn-font-size');
    
    // Remove active class from all buttons
    buttons.forEach(btn => btn.classList.remove('active'));
    
    // Add active class to selected button
    const selectedBtn = document.querySelector(`.btn-font-size[data-size="${size}"]`);
    if (selectedBtn) {
        selectedBtn.classList.add('active');
    }
    
    // Apply font size
    if (size === 'small') {
        dashboard.setAttribute('data-font-size', 'small');
    } else if (size === 'medium') {
        dashboard.removeAttribute('data-font-size');
    } else if (size === 'large') {
        dashboard.setAttribute('data-font-size', 'large');
    } else if (size === 'xlarge') {
        dashboard.setAttribute('data-font-size', 'xlarge');
    }
    
    localStorage.setItem('accessibility_fontSize', size);
}

// Personal Notes
function setupPersonalNotes() {
    // Notes are stored in localStorage for now
    loadNotes();
}

function openNoteEditor() {
    const editor = document.getElementById('notesEditor');
    if (editor) {
        editor.style.display = 'block';
        const textarea = document.getElementById('notesTextarea');
        if (textarea) {
            textarea.focus();
        }
    }
}

function saveNote() {
    const textarea = document.getElementById('notesTextarea');
    const editor = document.getElementById('notesEditor');
    const notesList = document.getElementById('notesList');
    
    if (textarea && textarea.value.trim()) {
        const note = textarea.value.trim();
        const notes = JSON.parse(localStorage.getItem('patient_notes') || '[]');
        notes.push({
            id: Date.now().toString(),
            content: note,
            createdAt: new Date().toISOString()
        });
        localStorage.setItem('patient_notes', JSON.stringify(notes));
        
        // Update UI
        if (editor) {
            editor.style.display = 'none';
        }
        if (textarea) {
            textarea.value = '';
        }
        
        loadNotes();
        showNotification('Note enregistrée', 'success');
    }
}

function cancelNote() {
    const editor = document.getElementById('notesEditor');
    const textarea = document.getElementById('notesTextarea');
    
    if (editor) {
        editor.style.display = 'none';
    }
    if (textarea) {
        textarea.value = '';
    }
}

function loadNotes() {
    const notes = JSON.parse(localStorage.getItem('patient_notes') || '[]');
    const notesList = document.getElementById('notesList');
    
    if (!notesList) return;
    
    if (notes.length === 0) {
        notesList.innerHTML = `
            <div class="note-item">
                <div class="note-content">
                    <p>Votre carnet personnel pour vos notes privées</p>
                </div>
                <div class="note-actions">
                    <button class="btn-note-edit" onclick="openNoteEditor()">
                        <i class="bi bi-pencil"></i>
                    </button>
                </div>
            </div>
        `;
    } else {
        notesList.innerHTML = notes.map(note => `
            <div class="note-item">
                <div class="note-content">
                    <p>${escapeHtml(note.content)}</p>
                    <small style="color: #64748B;">${new Date(note.createdAt).toLocaleDateString('fr-FR')}</small>
                </div>
                <div class="note-actions">
                    <button class="btn-note-edit" onclick="editNote('${note.id}')">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-note-delete" onclick="deleteNote('${note.id}')">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    }
}

function editNote(noteId) {
    if (!noteId) {
        openNoteEditor();
        return;
    }
    
    const notes = JSON.parse(localStorage.getItem('patient_notes') || '[]');
    const note = notes.find(n => n.id === noteId);
    
    if (note) {
        const textarea = document.getElementById('notesTextarea');
        const editor = document.getElementById('notesEditor');
        
        if (textarea && editor) {
            textarea.value = note.content;
            editor.style.display = 'block';
            textarea.focus();
            
            // Update save button to update instead of create
            const saveBtn = document.querySelector('.btn-save-note');
            if (saveBtn) {
                saveBtn.onclick = () => updateNote(noteId);
            }
        }
    }
}

function updateNote(noteId) {
    const textarea = document.getElementById('notesTextarea');
    const editor = document.getElementById('notesEditor');
    
    if (textarea && textarea.value.trim()) {
        const notes = JSON.parse(localStorage.getItem('patient_notes') || '[]');
        const noteIndex = notes.findIndex(n => n.id === noteId);
        
        if (noteIndex !== -1) {
            notes[noteIndex].content = textarea.value.trim();
            notes[noteIndex].updatedAt = new Date().toISOString();
            localStorage.setItem('patient_notes', JSON.stringify(notes));
            
            if (editor) {
                editor.style.display = 'none';
            }
            if (textarea) {
                textarea.value = '';
            }
            
            loadNotes();
            showNotification('Note mise à jour', 'success');
        }
    }
}

function deleteNote(noteId) {
    if (confirm('Êtes-vous sûr de vouloir supprimer cette note ?')) {
        const notes = JSON.parse(localStorage.getItem('patient_notes') || '[]');
        const filteredNotes = notes.filter(n => n.id !== noteId);
        localStorage.setItem('patient_notes', JSON.stringify(filteredNotes));
        loadNotes();
        showNotification('Note supprimée', 'success');
    }
}

// Trusted Contacts
function openAddContactModal() {
    const name = prompt('Nom du contact :');
    if (!name) return;
    
    const relationship = prompt('Relation (ex: Famille, Voisin) :');
    if (!relationship) return;
    
    const phone = prompt('Numéro de téléphone :');
    if (!phone) return;
    
    // Save to server
    fetch('/Patient/AddTrustedContact', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: `name=${encodeURIComponent(name)}&relationship=${encodeURIComponent(relationship)}&phoneNumber=${encodeURIComponent(phone)}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification('Contact ajouté avec succès', 'success');
            // Reload page to show new contact
            setTimeout(() => location.reload(), 1000);
        } else {
            showNotification(data.message || 'Erreur lors de l\'ajout du contact', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Erreur lors de l\'ajout du contact', 'error');
    });
}

function callContact(phone) {
    if (confirm(`Appeler ${phone} ?`)) {
        window.location.href = `tel:${phone}`;
    }
}

function removeContact(name, phone) {
    if (!confirm(`Êtes-vous sûr de vouloir supprimer le contact "${name}" ?`)) {
        return;
    }
    
    fetch('/Patient/RemoveTrustedContact', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: `name=${encodeURIComponent(name)}&phoneNumber=${encodeURIComponent(phone)}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification('Contact supprimé avec succès', 'success');
            // Reload page to update contact list
            setTimeout(() => location.reload(), 1000);
        } else {
            showNotification(data.message || 'Erreur lors de la suppression du contact', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Erreur lors de la suppression du contact', 'error');
    });
}

// Help Modals
function showHelpModal(type) {
    const modal = document.getElementById('helpModal');
    const body = document.getElementById('helpModalBody');
    
    if (!modal || !body) return;
    
    const helpContent = {
        request: {
            title: 'Comment demander de l\'aide',
            content: `
                <h3>Comment demander de l'aide</h3>
                <p>Pour demander de l'aide :</p>
                <ol>
                    <li>Cliquez sur "Demander de l'aide" en haut de la page</li>
                    <li>Choisissez le type d'aide dont vous avez besoin</li>
                    <li>Remplissez le formulaire avec les détails</li>
                    <li>Confirmez votre demande</li>
                </ol>
                <p>Un aidant sera automatiquement assigné à votre demande.</p>
            `
        },
        noResponse: {
            title: 'Que faire si personne ne répond',
            content: `
                <h3>Que faire si personne ne répond</h3>
                <p>Si aucun aidant ne répond à votre demande :</p>
                <ul>
                    <li>Vérifiez que votre demande est bien visible</li>
                    <li>Attendez quelques heures (les aidants peuvent être occupés)</li>
                    <li>Contactez le support si la demande est urgente</li>
                    <li>Essayez de modifier l'urgence ou la description de votre demande</li>
                </ul>
                <p>Pour les urgences, contactez directement les services d'urgence : 15 ou 112</p>
            `
        },
        cancel: {
            title: 'Annuler une demande',
            content: `
                <h3>Annuler une demande</h3>
                <p>Pour annuler une demande :</p>
                <ol>
                    <li>Allez dans "Mes demandes"</li>
                    <li>Trouvez la demande que vous souhaitez annuler</li>
                    <li>Cliquez sur "Annuler"</li>
                    <li>Confirmez l'annulation</li>
                </ol>
                <p><strong>Note :</strong> Vous ne pouvez pas annuler une demande déjà en cours.</p>
            `
        },
        safety: {
            title: 'Sécurité & respect',
            content: `
                <h3>Sécurité & respect</h3>
                <p>Votre sécurité est notre priorité :</p>
                <ul>
                    <li>Tous les aidants sont vérifiés</li>
                    <li>Vos données personnelles sont protégées</li>
                    <li>Vous pouvez masquer votre adresse exacte</li>
                    <li>Un système de notation permet de maintenir la qualité</li>
                </ul>
                <p>En cas de problème, contactez immédiatement le support ou les autorités.</p>
            `
        }
    };
    
    const content = helpContent[type];
    if (content) {
        body.innerHTML = content.content;
        modal.style.display = 'flex';
    }
}

function closeHelpModal() {
    const modal = document.getElementById('helpModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

// Close modal on outside click
document.addEventListener('click', function(e) {
    const modal = document.getElementById('helpModal');
    if (modal && e.target === modal) {
        closeHelpModal();
    }
});

// Cancel Request
function cancelRequest(requestId) {
    if (confirm('Êtes-vous sûr de vouloir annuler cette demande ?')) {
        // In a real implementation, this would send to the server
        fetch(`/Request/Cancel/${requestId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        })
        .then(response => {
            if (response.ok) {
                showNotification('Demande annulée', 'success');
                location.reload();
            } else {
                showNotification('Erreur lors de l\'annulation', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showNotification('Erreur lors de l\'annulation', 'error');
        });
    }
}

// Smooth Scrolling
function setupSmoothScrolling() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            const href = this.getAttribute('href');
            if (href !== '#' && href.length > 1) {
                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });
}

// Load Saved State
function loadSavedState() {
    // Font size
    const savedFontSize = localStorage.getItem('accessibility_fontSize');
    if (savedFontSize && savedFontSize !== 'medium') {
        changeFontSize(savedFontSize);
    }
    
    // High contrast
    const highContrast = localStorage.getItem('accessibility_highContrast');
    if (highContrast === 'true') {
        const checkbox = document.getElementById('highContrast');
        if (checkbox) {
            checkbox.checked = true;
            checkbox.dispatchEvent(new Event('change'));
        }
    }
    
    // Large buttons
    const largeButtons = localStorage.getItem('accessibility_largeButtons');
    if (largeButtons === 'true') {
        const checkbox = document.getElementById('largeButtons');
        if (checkbox) {
            checkbox.checked = true;
            checkbox.dispatchEvent(new Event('change'));
        }
    }
    
    // Tranquility mode
    const notificationsPaused = localStorage.getItem('tranquility_notificationsPaused');
    if (notificationsPaused === 'true') {
        const checkbox = document.getElementById('notificationsPaused');
        if (checkbox) {
            checkbox.checked = true;
        }
    }
    
    const proposalsBlocked = localStorage.getItem('tranquility_proposalsBlocked');
    if (proposalsBlocked === 'true') {
        const checkbox = document.getElementById('proposalsBlocked');
        if (checkbox) {
            checkbox.checked = true;
        }
    }
}

// Notification Helper
function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 1rem 1.5rem;
        background: ${type === 'success' ? '#10B981' : type === 'error' ? '#EF4444' : '#0EA5E9'};
        color: white;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        z-index: 3000;
        animation: slideIn 0.3s ease;
    `;
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, 3000);
}

// Add animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(400px);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);

// Escape HTML helper
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}





