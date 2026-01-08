// Real-time Aidant Tracking for Patients
let trackingMap = null;
let trackingMarker = null;
let trackingInterval = null;

async function initAidantTracking(requestId) {
    if (!requestId) return;
    
    // Create tracking modal
    const modal = document.getElementById('aidantTrackingModal');
    if (!modal) {
        createTrackingModal();
    }
    
    document.getElementById('aidantTrackingModal').style.display = 'flex';
    document.getElementById('trackingRequestId').value = requestId;
    
    // Initialize map
    setTimeout(() => {
        initTrackingMap(requestId);
    }, 300);
    
    // Start polling for location updates
    startTrackingPolling(requestId);
}

function createTrackingModal() {
    const modal = document.createElement('div');
    modal.id = 'aidantTrackingModal';
    modal.className = 'tracking-modal';
    modal.innerHTML = `
        <div class="tracking-modal-content">
            <div class="tracking-modal-header">
                <h3>Suivi en temps réel</h3>
                <button class="modal-close" onclick="closeTrackingModal()">&times;</button>
            </div>
            <div class="tracking-info-bar">
                <div class="tracking-status">
                    <span class="status-dot" id="trackingStatusDot"></span>
                    <span id="trackingStatusText">Chargement...</span>
                </div>
                <div class="tracking-distance" id="trackingDistance">
                    -- km
                </div>
            </div>
            <div id="trackingMap" style="height: 400px; width: 100%; border-radius: 12px; margin: 1rem 0;"></div>
            <div class="tracking-actions">
                <button class="btn-secondary" onclick="closeTrackingModal()">Fermer</button>
            </div>
            <input type="hidden" id="trackingRequestId" />
        </div>
    `;
    document.body.appendChild(modal);
}

function initTrackingMap(requestId) {
    const mapContainer = document.getElementById('trackingMap');
    if (!mapContainer || trackingMap) return;
    
    // Initialize Leaflet map
    trackingMap = L.map('trackingMap').setView([48.8566, 2.3522], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(trackingMap);
    
    // Load initial location
    updateTrackingLocation(requestId);
}

async function updateTrackingLocation(requestId) {
    try {
        const response = await fetch(`/Mission/GetAidantLocation?requestId=${encodeURIComponent(requestId)}`);
        const data = await response.json();
        
        if (data.success && data.aidantLocation && data.destinationLocation) {
            const aidantLat = data.aidantLocation.Coordinates[1];
            const aidantLon = data.aidantLocation.Coordinates[0];
            const destLat = data.destinationLocation.Coordinates[1];
            const destLon = data.destinationLocation.Coordinates[0];
            
            // Update map
            if (trackingMap) {
                // Clear existing markers
                trackingMap.eachLayer(layer => {
                    if (layer instanceof L.Marker || layer instanceof L.Circle) {
                        trackingMap.removeLayer(layer);
                    }
                });
                
                // Add destination marker
                L.marker([destLat, destLon], {
                    icon: L.icon({
                        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-red.png',
                        iconSize: [25, 41],
                        iconAnchor: [12, 41]
                    })
                }).addTo(trackingMap).bindPopup('Destination');
                
                // Add aidant marker
                trackingMarker = L.marker([aidantLat, aidantLon], {
                    icon: L.icon({
                        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-blue.png',
                        iconSize: [25, 41],
                        iconAnchor: [12, 41]
                    })
                }).addTo(trackingMap).bindPopup('Aidant');
                
                // Add route line
                L.polyline([[aidantLat, aidantLon], [destLat, destLon]], {
                    color: '#0EA5E9',
                    weight: 3,
                    opacity: 0.7
                }).addTo(trackingMap);
                
                // Fit bounds
                trackingMap.fitBounds([[aidantLat, aidantLon], [destLat, destLon]], { padding: [50, 50] });
            }
            
            // Calculate distance
            const distance = calculateDistance(aidantLat, aidantLon, destLat, destLon);
            document.getElementById('trackingDistance').textContent = distance.toFixed(1) + ' km';
            
            // Update status
            const statusDot = document.getElementById('trackingStatusDot');
            const statusText = document.getElementById('trackingStatusText');
            
            if (data.isAidantOnSite) {
                statusDot.className = 'status-dot status-on-site';
                statusText.textContent = 'Aidant sur place';
            } else {
                statusDot.className = 'status-dot status-en-route';
                statusText.textContent = 'Aidant en route';
            }
        }
    } catch (error) {
        console.error('Error updating tracking:', error);
    }
}

function startTrackingPolling(requestId) {
    // Update every 10 seconds
    trackingInterval = setInterval(() => {
        updateTrackingLocation(requestId);
    }, 10000);
}

function closeTrackingModal() {
    if (trackingInterval) {
        clearInterval(trackingInterval);
        trackingInterval = null;
    }
    document.getElementById('aidantTrackingModal').style.display = 'none';
}

function calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371; // Earth radius in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
              Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
              Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c;
}

// Emergency Hub - Contact Primary Caretaker
async function contactPrimaryCaretaker(requestId) {
    if (!confirm('Voulez-vous contacter votre contact d\'urgence et partager le statut de la mission actuelle ?')) {
        return;
    }
    
    try {
        const response = await fetch(`/Patient/ContactEmergency?requestId=${encodeURIComponent(requestId)}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '',
                'Content-Type': 'application/json'
            }
        });
        
        const result = await response.json();
        if (result.success) {
            alert('✅ Votre contact d\'urgence a été notifié avec le statut de la mission.');
        } else {
            alert('Erreur: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Erreur lors de la notification');
    }
}

// Verify Mission with Code
async function verifyMissionWithCode(requestId) {
    const code = prompt('Entrez le code de vérification à 4 chiffres fourni par l\'aidant:');
    if (!code || code.length !== 4) {
        alert('Code invalide. Veuillez entrer un code à 4 chiffres.');
        return;
    }
    
    try {
        const response = await fetch(`/Mission/VerifyMission?requestId=${encodeURIComponent(requestId)}&code=${code}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '',
                'Content-Type': 'application/json'
            }
        });
        
        const result = await response.json();
        if (result.success) {
            alert('✅ Mission vérifiée et complétée avec succès !');
            location.reload();
        } else {
            alert('Erreur: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Erreur lors de la vérification');
    }
}





