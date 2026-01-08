// Mission Completion with Proof Upload
async function completeMissionWithProof(requestId) {
    const modal = document.getElementById('missionCompletionModal');
    if (!modal) {
        createMissionCompletionModal();
    }
    
    document.getElementById('missionCompletionModal').style.display = 'flex';
    document.getElementById('completionRequestId').value = requestId;
    
    // Generate verification code
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const response = await fetch(`/Mission/GenerateVerificationCode?requestId=${encodeURIComponent(requestId)}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            });
        const result = await response.json();
        if (result.success) {
            document.getElementById('verificationCodeDisplay').textContent = result.code;
        }
    } catch (error) {
        console.error('Error generating code:', error);
    }
}

function createMissionCompletionModal() {
    const modal = document.createElement('div');
    modal.id = 'missionCompletionModal';
    modal.className = 'mission-modal';
    modal.innerHTML = `
        <div class="mission-modal-content">
            <div class="mission-modal-header">
                <h3>Compléter la mission</h3>
                <button class="modal-close" onclick="closeMissionModal()">&times;</button>
            </div>
            <form id="missionCompletionForm" enctype="multipart/form-data">
                <input type="hidden" id="completionRequestId" name="requestId" />
                
                <div class="form-group">
                    <label>Preuve de livraison *</label>
                    <div class="file-upload-area" id="fileUploadArea">
                        <input type="file" id="proofFile" name="file" accept="image/*,.pdf" required />
                        <div class="file-upload-placeholder">
                            <i class="bi bi-cloud-upload"></i>
                            <p>Cliquez pour télécharger une photo ou un reçu</p>
                            <span>JPG, PNG ou PDF (max 10MB)</span>
                        </div>
                        <div class="file-preview" id="filePreview" style="display: none;">
                            <img id="previewImage" src="" alt="Preview" />
                            <span id="fileName"></span>
                            <button type="button" onclick="removeFile()" class="remove-file-btn">
                                <i class="bi bi-x"></i>
                            </button>
                        </div>
                    </div>
                </div>
                
                <div class="form-group">
                    <label>Type de preuve</label>
                    <select id="proofType" name="proofType" class="form-control">
                        <option value="Photo">Photo de l'article livré</option>
                        <option value="Receipt">Reçu numérique</option>
                        <option value="Signature">Signature</option>
                    </select>
                </div>
                
                <div class="verification-code-section">
                    <label>Code de vérification</label>
                    <div class="verification-code-display">
                        <span id="verificationCodeDisplay">----</span>
                        <small>Donnez ce code au patient pour vérifier la livraison</small>
                    </div>
                </div>
                
                <div class="modal-actions">
                    <button type="button" class="btn-secondary" onclick="closeMissionModal()">Annuler</button>
                    <button type="submit" class="btn-primary">Compléter la mission</button>
                </div>
            </form>
        </div>
    `;
    document.body.appendChild(modal);
    
    // File upload handling
    document.getElementById('proofFile').addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (file) {
            const preview = document.getElementById('filePreview');
            const placeholder = document.querySelector('.file-upload-placeholder');
            const fileName = document.getElementById('fileName');
            
            if (file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function(e) {
                    document.getElementById('previewImage').src = e.target.result;
                    preview.style.display = 'block';
                    placeholder.style.display = 'none';
                };
                reader.readAsDataURL(file);
            } else {
                fileName.textContent = file.name;
                preview.style.display = 'block';
                placeholder.style.display = 'none';
            }
        }
    });
    
    // Form submission
    document.getElementById('missionCompletionForm').addEventListener('submit', async function(e) {
        e.preventDefault();
        const formData = new FormData();
        const fileInput = document.getElementById('proofFile');
        const requestId = document.getElementById('completionRequestId').value;
        const proofType = document.getElementById('proofType').value;
        
        if (!fileInput.files[0]) {
            alert('Veuillez sélectionner un fichier');
            return;
        }
        
        formData.append('file', fileInput.files[0]);
        
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const response = await fetch(`/Mission/UploadProof?requestId=${encodeURIComponent(requestId)}&proofType=${encodeURIComponent(proofType)}`, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token
                }
            });
            
            const result = await response.json();
            if (result.success) {
                alert('Preuve téléchargée avec succès. Le code de vérification est: ' + result.verificationCode);
                closeMissionModal();
                location.reload();
            } else {
                alert('Erreur: ' + result.message);
            }
        } catch (error) {
            console.error('Error:', error);
            alert('Erreur lors du téléchargement');
        }
    });
}

function removeFile() {
    document.getElementById('proofFile').value = '';
    document.getElementById('filePreview').style.display = 'none';
    document.querySelector('.file-upload-placeholder').style.display = 'block';
}

function closeMissionModal() {
    document.getElementById('missionCompletionModal').style.display = 'none';
}

// GPS Check-in
async function checkInWithGPS(requestId) {
    if (!navigator.geolocation) {
        alert('La géolocalisation n\'est pas supportée par votre navigateur');
        return;
    }
    
    navigator.geolocation.getCurrentPosition(
        async function(position) {
            const latitude = position.coords.latitude;
            const longitude = position.coords.longitude;
            
            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
                const response = await fetch(`/Mission/CheckIn?requestId=${encodeURIComponent(requestId)}&latitude=${latitude}&longitude=${longitude}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                });
                
                const result = await response.json();
                if (result.success) {
                    if (result.isWithinRadius) {
                        alert('✅ Vous êtes arrivé à destination! Le patient a été notifié.');
                    } else {
                        alert(`📍 Check-in effectué. Distance: ${result.distance}m de la destination`);
                    }
                } else {
                    alert('Erreur: ' + result.message);
                }
            } catch (error) {
                console.error('Error:', error);
                alert('Erreur lors du check-in');
            }
        },
        function(error) {
            alert('Erreur de géolocalisation: ' + error.message);
        }
    );
}

// Report Safety Incident
function reportSafetyIncident(requestId) {
    const incidentType = prompt('Type d\'incident:\n1. Emergency\n2. Medical\n3. Safety\n4. Other');
    if (!incidentType) return;
    
    const severity = prompt('Sévérité:\n1. Low\n2. Medium\n3. High\n4. Critical');
    if (!severity) return;
    
    const description = prompt('Description de l\'incident:');
    if (!description) return;
    
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            async function(position) {
                await submitIncident(requestId, incidentType, severity, description, 
                    position.coords.latitude, position.coords.longitude);
            },
            async function() {
                await submitIncident(requestId, incidentType, severity, description);
            }
        );
    } else {
        submitIncident(requestId, incidentType, severity, description);
    }
}

async function submitIncident(requestId, incidentType, severity, description, latitude = null, longitude = null) {
    try {
        let url = `/Mission/ReportIncident?requestId=${requestId}&incidentType=${incidentType}&severity=${severity}&description=${encodeURIComponent(description)}`;
        if (latitude && longitude) {
            url += `&latitude=${latitude}&longitude=${longitude}`;
        }
        
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const result = await response.json();
        if (result.success) {
            alert('✅ Incident signalé avec succès. Les administrateurs ont été notifiés.');
        } else {
            alert('Erreur: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Erreur lors du signalement');
    }
}

