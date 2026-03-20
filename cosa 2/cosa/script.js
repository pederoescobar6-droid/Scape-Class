let games = [];
let currentIndex = 0;
const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');


document.addEventListener('DOMContentLoaded', () => {
  createStarfield();
  loadGamesFromXML();
  resize();
});

window.addEventListener('resize', resize);

function resize() {
  canvas.width = window.innerWidth;
  canvas.height = window.innerHeight;
}

const audioCtx = new (window.AudioContext || window.webkitAudioContext)();

function playSynthSound(type) {
    if (audioCtx.state === 'suspended') audioCtx.resume();
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    osc.connect(gain);
    gain.connect(audioCtx.destination);
    const now = audioCtx.currentTime;

    if (type === 'move') {
        osc.type = 'sine';
        osc.frequency.setValueAtTime(880, now);
        osc.frequency.exponentialRampToValueAtTime(440, now + 0.1);
        gain.gain.setValueAtTime(0.1, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.1);
        osc.start(); osc.stop(now + 0.1);
    } else if (type === 'select') {
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(110, now);
        osc.frequency.exponentialRampToValueAtTime(880, now + 0.5);
        gain.gain.setValueAtTime(0.2, now);
        gain.gain.linearRampToValueAtTime(0, now + 0.5);
        osc.start(); osc.stop(now + 0.5);
    }
}

function createStarfield() {
  const starfield = document.getElementById('starfield');
  if(!starfield) return;
  const numStars = 200;

  for (let i = 0; i < numStars; i++) {
    const star = document.createElement('div');
    star.style.position = 'absolute';
    star.style.width = Math.random() * 3 + 'px';
    star.style.height = star.style.width;
    star.style.background = 'white';
    star.style.left = Math.random() * 100 + '%';
    star.style.top = Math.random() * 100 + '%';
    star.style.animation = `twinkle ${Math.random() * 3 + 2}s infinite`;
    star.style.animationDelay = Math.random() * 2 + 's';
    starfield.appendChild(star);
  }
}

function loadGamesFromXML() {
  fetch('games.xml')
    .then(response => response.text())
    .then(str => (new window.DOMParser()).parseFromString(str, 'text/xml'))
    .then(xml => {
      const juegosNode = xml.getElementsByTagName('juegos')[0];
      games = [];

      for (let i = 1; i <= 4; i++) {
        const juegoNode = juegosNode.getElementsByTagName(`juego${i}`)[0];
        if (!juegoNode) continue;
        
        const habilitado = juegoNode.getElementsByTagName('habilitado')[0].textContent.trim().toLowerCase() === 'si';
        if (!habilitado) continue;

        const nombre = juegoNode.getElementsByTagName('nombre')[0].textContent.trim();
        const ruta = juegoNode.getElementsByTagName('ruta')[0].textContent.trim();

        games.push({ nombre: nombre, path: ruta, color: '#00f2ff' });
      }

      if (games.length > 0) {
        setupKeyboardControls();
        animate();
      }
    })
    .catch(err => console.error('Error cargando XML:', err));
}

function drawRhombus(x, y, size) {
    ctx.beginPath();
    ctx.moveTo(x, y - size);
    ctx.lineTo(x + size, y);
    ctx.lineTo(x, y + size);
    ctx.lineTo(x - size, y);
    ctx.closePath();
}

function animate() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    const cx = canvas.width / 2;
    const cy = canvas.height * 0.35; 
    const espacio = 320;

    games.forEach((juego, i) => {
        const posX = cx + (i - currentIndex) * espacio;
        const distancia = Math.abs(i - currentIndex);
        const escala = Math.max(0.4, 1 - distancia * 0.4);
        const opacidad = Math.max(0.1, 1 - distancia * 0.7);
        const size = 140 * escala; 

        ctx.save();
        ctx.globalAlpha = opacidad;
        ctx.shadowBlur = (i === currentIndex) ? 40 : 10;
        ctx.shadowColor = juego.color;
        ctx.strokeStyle = juego.color;
        ctx.lineWidth = (i === currentIndex) ? 6 : 2;
        
        drawRhombus(posX, cy, size);
        ctx.stroke();

        ctx.fillStyle = '#050510';
        ctx.fill();

        ctx.fillStyle = "white";
        ctx.font = `bold ${22 * escala}px 'Orbitron', sans-serif`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(juego.nombre, posX, cy);
        ctx.restore();
    });

    requestAnimationFrame(animate);
}

function setupKeyboardControls() {
  document.addEventListener('keydown', (e) => {
    if (!games.length) return;

    switch(e.key.toLowerCase()) {
      case 'arrowleft':
      case 'a':
        currentIndex = (currentIndex - 1 + games.length) % games.length;
        break;
      case 'arrowright':
      case 'd':
        currentIndex = (currentIndex + 1) % games.length;
        break;
      case 'enter':
      case ' ':
        e.preventDefault();
        launchGame();
        break;
    }
  });
}

function launchGame() {
  const game = games[currentIndex];
  if (game && game.path) {
    let finalPath = game.path.replace('file://', '').trim();
    
    console.log("Intentando cargar:", finalPath);
    
    // Redirigir
    window.location.href = finalPath;
  } else {
    console.error("No se encontró una ruta válida para este juego.");
  }
}

function setupKeyboardControls() {
  document.addEventListener('keydown', (e) => {
    if (!games.length) return;

    switch(e.key.toLowerCase()) {
      case 'arrowleft':
      case 'a':
        currentIndex = (currentIndex - 1 + games.length) % games.length;
        break;
      case 'arrowright':
      case 'd':
        currentIndex = (currentIndex + 1) % games.length;
        break;
      case 'enter':
      case ' ':
        e.preventDefault();
        const path = games[currentIndex].path.replace('file://', '');
        window.location.href = path;
        break;
    }
  });
}

let smoothIndex = 0;

const originalAnimate = animate;

animate = function() {
    smoothIndex += (currentIndex - smoothIndex) * 0.1;

    const backupIndex = currentIndex;

    currentIndex = smoothIndex;

    originalAnimate();

    currentIndex = backupIndex;
};


const originalAnimateWithFullSync = animate;

animate = function() {
    const originalDrawRhombus = drawRhombus;
    const originalFillText = ctx.fillText;

    const time = Date.now() * 0.002;

    drawRhombus = function(x, y, size) {
        const index = Math.round((x - (canvas.width / 2)) / 320 + (typeof visualIndex !== 'undefined' ? visualIndex : currentIndex));
        const hoverOffset = Math.sin(time + index) * 10;
        ctx.save();
        ctx.translate(0, hoverOffset);
        
        originalDrawRhombus(x, y, size);

    };

    ctx.fillText = function(text, x, y) {
        originalFillText.apply(this, arguments);
        ctx.restore();
    };

    originalAnimateWithFullSync();

    drawRhombus = originalDrawRhombus;
    ctx.fillText = originalFillText;
};


const originalAnimateWithZoom = animate;

animate = function() {
    const originalShadowBlur = ctx.shadowBlur;
    const originalLineWidth = ctx.lineWidth;

    const originalDraw = drawRhombus;
    
    drawRhombus = function(x, y, size) {
        const centerX = canvas.width / 2;
        const distAlCentro = Math.abs(x - centerX);
        
        const proximidad = Math.max(0, 1 - (distAlCentro / 320)); 

        const nuevoSize = size * (1 + proximidad * 0.2);
        
        ctx.shadowBlur = 15 + (proximidad * 45);
        
        ctx.lineWidth = 2 + (proximidad * 4);

        originalDraw(x, y, nuevoSize);
    };

    originalAnimateWithZoom();

    drawRhombus = originalDraw;
    ctx.shadowBlur = originalShadowBlur;
    ctx.lineWidth = originalLineWidth;
};

const paletaNeon = ['#ff00de', '#00d4ff', '#ffb800', '#00ff95'];

function aplicarColoresDiferentes() {
    if (games.length > 0) {
        games.forEach((juego, i) => {
            juego.color = paletaNeon[i % paletaNeon.length];
        });
    }
}

const originalAnimateDefinitivo = animate;
animate = function() {
    if (games.length > 0 && (!games[0].color || games[0].color === '#00f2ff')) {
        aplicarColoresDiferentes();
    }

    if (games[currentIndex]) {
        const colorActual = games[currentIndex].color || '#00f2ff';
        
        ctx.save();
        const gradient = ctx.createRadialGradient(
            canvas.width / 2, canvas.height * 0.35, 0,
            canvas.width / 2, canvas.height * 0.35, canvas.width * 0.7
        );
        
        gradient.addColorStop(0, colorActual + "26"); 
        gradient.addColorStop(1, "transparent");
        
        ctx.fillStyle = gradient;
        ctx.globalCompositeOperation = "screen";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.restore();
    }

    originalAnimateDefinitivo();
};
