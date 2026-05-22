// Reliability Heatmap Generator (GitHub-style)
function generateReliabilityHeatmap(aidantId) {
    const heatmapContainer = document.getElementById('reliabilityHeatmap');
    if (!heatmapContainer) return;

    // Generate last 6 months of data (26 weeks)
    const weeks = 26;
    const daysPerWeek = 7;
    const totalDays = weeks * daysPerWeek;
    
    // For demo purposes, generate random activity data
    // In production, this would come from actual mission completion data
    const activityData = generateActivityData(totalDays);
    
    // Create heatmap grid
    heatmapContainer.innerHTML = '';
    
    for (let day = 0; day < totalDays; day++) {
        const dayElement = document.createElement('div');
        dayElement.className = `heatmap-day level-${activityData[day]}`;
        dayElement.title = `Jour ${day + 1} - Niveau d'activité: ${activityData[day]}`;
        heatmapContainer.appendChild(dayElement);
    }
}

// Generate activity data (0-4 levels)
// In production, this would query actual mission completion dates
function generateActivityData(totalDays) {
    const data = [];
    const today = new Date();
    
    for (let i = 0; i < totalDays; i++) {
        const date = new Date(today);
        date.setDate(date.getDate() - (totalDays - i));
        
        // Simulate activity: more activity on weekdays, less on weekends
        const dayOfWeek = date.getDay();
        let baseLevel = 0;
        
        if (dayOfWeek >= 1 && dayOfWeek <= 5) {
            // Weekday: 20% chance of activity
            baseLevel = Math.random() < 0.2 ? Math.floor(Math.random() * 3) + 1 : 0;
        } else {
            // Weekend: 10% chance of activity
            baseLevel = Math.random() < 0.1 ? Math.floor(Math.random() * 2) + 1 : 0;
        }
        
        data.push(baseLevel);
    }
    
    return data;
}

// Initialize heatmap on page load
document.addEventListener('DOMContentLoaded', function() {
    const aidantId = document.querySelector('[data-aidant-id]')?.getAttribute('data-aidant-id');
    if (aidantId) {
        generateReliabilityHeatmap(aidantId);
    } else {
        // Fallback: generate with demo data
        generateReliabilityHeatmap('demo');
    }
});






