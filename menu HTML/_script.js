@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@700;900&display=swap');

* { margin: 0; padding: 0; box-sizing: border-box; }

body, html {
    height: 100vh;
    width: 100vw;
    overflow: hidden;
    background-color: #030205;
    display: flex;
    flex-direction: column;
    align-items: center;
    font-family: 'Orbitron', sans-serif;
}

.overlay-texture {
    position: fixed;
    top: 0; left: 0;
    width: 100%; height: 100%;
    background: radial-gradient(circle, transparent 20%, rgba(0,0,0,0.8) 100%),
                url('https://grainy-gradients.vercel.app/noise.svg');
    opacity: 0.12; /* Un poco más sutil */
    pointer-events: none;
    z-index: 100;
}

body::after {
    content: " ";
    position: fixed;
    top: 0; left: 0;
    width: 100%; height: 100%;
    background: linear-gradient(rgba(18, 16, 16, 0) 50%, rgba(0, 0, 0, 0.1) 50%);
    background-size: 100% 4px;
    z-index: 101;
    pointer-events: none;
}

.corner-ui {
    position: fixed;
    color: #00d4ff;
    font-size: 9px;
    opacity: 0.4;
    padding: 30px;
    z-index: 20;
    line-height: 1.8;
    letter-spacing: 3px;
    text-transform: uppercase;
}
.top-left { top: 0; left: 0; }
.bottom-right { bottom: 0; right: 0; text-align: right; }

.stars-container {
    position: fixed;
    top: 0; left: 0;
    width: 100vw; height: 100vh;
    z-index: 0;
}

.star { position: absolute; background: white; border-radius: 50%; }

.retro-title {
    font-size: 6rem;
    margin-top: 40px;
    background: linear-gradient(to bottom, #fff 0%, #ff00de 50%, #00d4ff 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    filter: drop-shadow(0 0 25px rgba(255, 0, 222, 0.6));
    z-index: 10;
    transition: transform 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    user-select: none;
}

.canvas-wrapper { width: 100%; flex-grow: 1; display: flex; justify-content: center; align-items: center; z-index: 5; }

#menuCanvas { filter: drop-shadow(0 0 20px var(--glow-color, #00d4ff)); }

#ui-overlay {
    margin-bottom: 50px;
    background: rgba(10, 5, 20, 0.9);
    border-top: 1px solid rgba(0, 212, 255, 0.2);
    color: #00d4ff;
    padding: 15px 60px;
    font-size: 0.8rem;
    letter-spacing: 2px;
    z-index: 10;
    text-shadow: 0 0 10px #00d4ff;
    clip-path: polygon(10% 0, 90% 0, 100% 100%, 0% 100%); /* Forma trapezoidal de consola */
}

.glitch-active {
    animation: glitch-anim 0.08s infinite;
    filter: contrast(2) brightness(1.2) saturate(1.5) !important;
}

@keyframes glitch-anim {
    0% { clip-path: inset(15% 0 35% 0); transform: translate(-8px, 4px); }
    50% { clip-path: inset(35% 0 15% 0); transform: translate(8px, -4px); }
    100% { clip-path: inset(0 0 0 0); transform: translate(0); }
}
.flash-effect {
    position: absolute;
    inset: 0;
    background: white;
    opacity: 0;
    pointer-events: none;
    z-index: 100;
    transition: opacity 0.2s ease-in;
}

.flash-active {
    opacity: 1;
}