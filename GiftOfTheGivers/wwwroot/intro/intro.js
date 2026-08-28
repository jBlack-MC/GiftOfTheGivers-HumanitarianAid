/* ==========================================================================
   Gift of the Givers — intro particle system
   --------------------------------------------------------------------------
   Timeline (seconds):
     0.0 - 2.0   scattered particles drift in / swirl toward a loose cloud
     2.0 - 4.0   particles lock onto the compass silhouette; shards burst
     4.0 - 4.9   particles dissolve as the crisp SVG compass takes over
     3.7 - 8.0   CSS handles the symbol reveal, light sweep, title & subtitle
     8.0         `gotg:introComplete` fires; ambient sparkle keeps looping;
                 if reached via the homepage's once-per-session gate
                 (?auto=1) this hands off back to "/" after a brief hold —
                 otherwise (direct visit) it just sits here, replayable
   ========================================================================== */
(function () {
  "use strict";

  var stage   = document.getElementById("stage");
  var canvas  = document.getElementById("fx");
  var ctx     = canvas.getContext("2d");
  var host    = document.getElementById("compass");
  var btnSkip = document.getElementById("skip");
  var btnRep  = document.getElementById("replay");
  var btnTheme = document.getElementById("theme");

  var DURATION = 8.0;                       // seconds of scripted timeline
  var COUNT    = 300;                       // particles
  var reduced  = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  /* Reached via the homepage's once-per-session gate (?auto=1)? Then this
     run hands off back to "/" once it finishes/is skipped. Visited directly
     (no query param) it just behaves as a normal, replayable standalone
     page — no forced navigation. Either way the session is marked "seen"
     so the homepage gate won't redirect here again this session. */
  var AUTO = /(?:^|[?&])auto=1(?:&|$)/.test(location.search);
  function markIntroSeen() {
    try { sessionStorage.setItem("gotg-intro-seen", "1"); } catch (e) {}
  }
  function goHome(delayMs) {
    markIntroSeen();
    if (!AUTO) return;
    setTimeout(function () { location.href = "/"; }, delayMs || 0);
  }

  /* -------- compass geometry (also used to sample particle targets) -------
     Thick 4-point compass rose: wide blades tapering to sharp tips, with
     smaller secondary barbs on the diagonals. BASE = how far the blade
     shoulders sit from the centre (bigger = chunkier). */
  var LONG_R = 0.95, SHORT_R = 0.52, BASE = 0.40, SPREAD = 0.36;

  function compassRays() {
    var rays = [];
    for (var k = 0; k < 8; k++) {
      var ang = k * Math.PI / 4 - Math.PI / 2;
      var R   = (k % 2 === 0) ? LONG_R : SHORT_R;
      rays.push({
        tip: [Math.cos(ang) * R, Math.sin(ang) * R],
        bl:  [Math.cos(ang - SPREAD) * BASE, Math.sin(ang - SPREAD) * BASE],
        br:  [Math.cos(ang + SPREAD) * BASE, Math.sin(ang + SPREAD) * BASE]
      });
    }
    return rays;
  }
  var RAYS = compassRays();

  function buildCompassSVG() {
    var C = 100, a = "", b = "";
    for (var k = 0; k < 8; k++) {
      var ang = k * Math.PI / 4 - Math.PI / 2;
      var R   = ((k % 2 === 0) ? LONG_R : SHORT_R) * 100;
      if (k === 0) R += 4;                              // north point a touch longer
      var G   = BASE * 100;
      var tx  = C + Math.cos(ang) * R,          ty  = C + Math.sin(ang) * R;
      var blx = C + Math.cos(ang - SPREAD) * G, bly = C + Math.sin(ang - SPREAD) * G;
      var brx = C + Math.cos(ang + SPREAD) * G, bry = C + Math.sin(ang + SPREAD) * G;
      a += 'M' + tx + ' ' + ty + 'L' + blx + ' ' + bly + 'L' + C + ' ' + C + 'Z';
      b += 'M' + tx + ' ' + ty + 'L' + brx + ' ' + bry + 'L' + C + ' ' + C + 'Z';
    }
    return '<svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg">' +
             '<path class="c-b" d="' + b + '"/>' +
             '<path class="c-a" d="' + a + '"/>' +
             '<circle class="c-ring" cx="100" cy="100" r="12.5"/>' +
             '<circle class="c-core" cx="100" cy="100" r="6.5"/>' +
           '</svg>';
  }
  host.innerHTML = buildCompassSVG();

  /* -------- point-in-compass test -------------------------------------- */
  function triContains(px, py, a, b, c) {
    var d1 = (px - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (py - b[1]);
    var d2 = (px - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (py - c[1]);
    var d3 = (px - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (py - a[1]);
    var neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
    var pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
    return !(neg && pos);
  }
  function insideCompass(x, y) {
    if (x * x + y * y < BASE * BASE * 0.4) return true;
    for (var i = 0; i < RAYS.length; i++) {
      var r = RAYS[i];
      if (triContains(x, y, r.tip, r.bl, [0, 0])) return true;
      if (triContains(x, y, r.tip, r.br, [0, 0])) return true;
    }
    return false;
  }
  /* a normalised (-1..1) target point on the compass */
  function sampleTarget() {
    if (Math.random() < 0.58) {                 // silhouette edge
      var r = RAYS[(Math.random() * 8) | 0];
      var edge = Math.random() < 0.5 ? r.bl : r.br;
      var t = Math.random();
      return [r.tip[0] + (edge[0] - r.tip[0]) * t,
              r.tip[1] + (edge[1] - r.tip[1]) * t];
    }
    for (var i = 0; i < 40; i++) {              // area fill
      var x = Math.random() * 2 - 1, y = Math.random() * 2 - 1;
      if (insideCompass(x, y)) return [x, y];
    }
    return [0, 0];
  }

  /* -------- glow sprites, per theme ------------------------------------- */
  function makeSprite(color) {
    var s = 64, c = document.createElement("canvas");
    c.width = c.height = s;
    var g = c.getContext("2d");
    var rg = g.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    rg.addColorStop(0, color);
    rg.addColorStop(0.22, color);
    rg.addColorStop(1, "rgba(0,0,0,0)");
    g.fillStyle = rg;
    g.fillRect(0, 0, s, s);
    return c;
  }
  /* dark: bright particles, additive glow.  light: navy particles, normal blend. */
  var SPR = {
    dark: {
      main:  makeSprite("rgba(214,230,255,1)"),
      ember: makeSprite("rgba(255,152,90,1)"),
      shard: "rgba(232,240,255,1)",
      additive: true
    },
    light: {
      main:  makeSprite("rgba(30,62,120,1)"),
      ember: makeSprite("rgba(198,92,38,1)"),
      shard: "rgba(22,50,100,1)",
      additive: false
    }
  };

  var theme = "dark";
  function readTheme() {
    var attr = document.documentElement.getAttribute("data-theme");
    theme = (attr === "light" || attr === "dark")
      ? attr
      : (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
  }
  readTheme();

  /* -------- state ------------------------------------------------------- */
  var W = 0, H = 0, DPR = 1, U = 1;
  var cx = 0, cy = 0, logoR = 1;
  var particles = [], shards = [], sparks = [];
  var raf = 0, running = false, startTs = 0, clock = 0, endTimer = 0, done = false;

  function rand(a, b) { return a + Math.random() * (b - a); }
  function clamp01(v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
  function easeIO(t) { return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2; }
  function lerp(a, b, t) { return a + (b - a) * t; }

  function measure() {
    var sRect = stage.getBoundingClientRect();
    var cRect = host.getBoundingClientRect();
    DPR = Math.min(window.devicePixelRatio || 1, 2);
    W = sRect.width; H = sRect.height;
    U = W / 100;
    canvas.width  = Math.round(W * DPR);
    canvas.height = Math.round(H * DPR);
    ctx.setTransform(DPR, 0, 0, DPR, 0, 0);
    cx = (cRect.left - sRect.left) + cRect.width / 2;
    cy = (cRect.top  - sRect.top)  + cRect.height / 2;
    logoR = cRect.width / 2;
    if (!logoR) { cx = W / 2; cy = H / 2; logoR = W * 0.15; }  // CSS not ready yet
  }

  function build() {
    particles = [];
    var diag = Math.sqrt(W * W + H * H);
    for (var i = 0; i < COUNT; i++) {
      var tgt = sampleTarget();
      var ang = rand(0, Math.PI * 2);
      var dist = rand(0.32, 0.95) * diag / 2;
      var ember = Math.random() < 0.07;
      particles.push({
        hx: cx + Math.cos(ang) * dist,
        hy: cy + Math.sin(ang) * dist,
        tx: tgt[0],
        ty: tgt[1],
        cnx: tgt[0] * rand(1.5, 1.95) + rand(-0.16, 0.16),
        cny: tgt[1] * rand(1.5, 1.95) + rand(-0.16, 0.16),
        ph: rand(0, Math.PI * 2),
        amp: rand(0.6, 2.6),
        r: rand(0.55, 1.7) * (ember ? 1.15 : 1),
        emb: ember
      });
    }
    shards = [];
    for (var j = 0; j < 20; j++) {
      var sa = rand(0, Math.PI * 2), sv = rand(0.55, 1.35);
      shards.push({
        x0: Math.cos(sa) * rand(0.02, 0.14),
        y0: Math.sin(sa) * rand(0.02, 0.14),
        vx: Math.cos(sa) * sv,
        vy: Math.sin(sa) * sv,
        rot: rand(0, Math.PI * 2),
        vr: rand(-4, 4),
        sz: rand(0.9, 1.9)
      });
    }
    sparks = [];
    for (var s = 0; s < 46; s++) {
      sparks.push({
        a: rand(0, Math.PI * 2),
        d: rand(0.15, 1.45),
        ph: rand(0, Math.PI * 2),
        sp: rand(1.4, 3.4),
        r: rand(0.35, 0.9)
      });
    }
  }

  /* -------- drawing ---------------------------------------------------- */
  function drawParticle(p, x, y, alpha) {
    var spr = SPR[theme][p.emb ? "ember" : "main"];
    var d = p.r * U * 3.1;
    ctx.globalAlpha = alpha;
    ctx.drawImage(spr, x - d / 2, y - d / 2, d, d);
  }

  function render(t) {
    var set = SPR[theme];
    var glow = set.additive ? "lighter" : "source-over";
    ctx.clearRect(0, 0, W, H);

    /* particles ------------------------------------------------------- */
    ctx.globalCompositeOperation = t < 4.25 ? glow : "source-over";
    for (var i = 0; i < particles.length; i++) {
      var p = particles[i], x, y, alpha;

      if (t < 2) {
        var a = easeIO(clamp01(t / 2));
        var sw = (1 - a) * 0.9;
        var cs = Math.cos(sw), sn = Math.sin(sw);
        var nx = p.cnx * cs - p.cny * sn;
        var ny = p.cnx * sn + p.cny * cs;
        var px = cx + nx * logoR, py = cy + ny * logoR;
        var n = 1 - a;
        x = lerp(p.hx, px, a) + Math.sin(t * 3 + p.ph) * p.amp * U * n;
        y = lerp(p.hy, py, a) + Math.cos(t * 2.3 + p.ph) * p.amp * U * n;
        alpha = clamp01(t / 0.4) * 0.62;
      } else if (t < 4) {
        var b = easeIO(clamp01((t - 2) / 2));
        var fromX = cx + p.cnx * logoR, fromY = cy + p.cny * logoR;
        var toX   = cx + p.tx  * logoR, toY   = cy + p.ty  * logoR;
        var m = 1 - b;
        x = lerp(fromX, toX, b) + Math.sin(t * 5 + p.ph) * p.amp * U * 0.5 * m;
        y = lerp(fromY, toY, b) + Math.cos(t * 4 + p.ph) * p.amp * U * 0.5 * m;
        alpha = lerp(0.62, 0.9, b);
      } else {
        x = cx + p.tx * logoR;
        y = cy + p.ty * logoR;
        alpha = clamp01(1 - (t - 4) / 0.85) * 0.9;
        if (p.emb) alpha *= clamp01(1 - (t - 3.6) / 0.6);
      }
      if (alpha > 0.01) drawParticle(p, x, y, alpha);
    }

    /* shard burst (2.0 - 4.0s) ------------------------------------------- */
    if (t > 1.95 && t < 4.1) {
      ctx.globalCompositeOperation = glow;
      ctx.fillStyle = set.shard;
      for (var s = 0; s < shards.length; s++) {
        var sh = shards[s], st = t - 1.95;
        var sa = st < 0.28 ? st / 0.28 : clamp01(1 - (st - 0.28) / 1.75);
        if (sa <= 0) continue;
        var gx = cx + (sh.x0 + sh.vx * st * 0.9) * logoR;
        var gy = cy + (sh.y0 + sh.vy * st * 0.9) * logoR;
        var z = sh.sz * U;
        ctx.save();
        ctx.translate(gx, gy);
        ctx.rotate(sh.rot + sh.vr * st);
        ctx.globalAlpha = sa;
        ctx.beginPath();
        ctx.moveTo(0, -z); ctx.lineTo(z * 0.9, z * 0.7); ctx.lineTo(-z * 0.9, z * 0.7);
        ctx.closePath();
        ctx.fill();
        ctx.restore();
      }
    }

    /* lingering sparkle around the finished mark (3.4s+, loops forever) - */
    if (t > 3.4) {
      ctx.globalCompositeOperation = glow;
      var fade = clamp01((t - 3.4) / 0.8);
      for (var k = 0; k < sparks.length; k++) {
        var sp = sparks[k];
        var tw = 0.35 + 0.65 * (0.5 + 0.5 * Math.sin(t * sp.sp + sp.ph));
        var rr = sp.d * logoR + Math.sin(t * 0.6 + sp.ph) * U * 1.5;
        var xx = cx + Math.cos(sp.a + t * 0.05) * rr;
        var yy = cy + Math.sin(sp.a + t * 0.05) * rr;
        var dd = sp.r * U * 3.4;
        ctx.globalAlpha = tw * fade * 0.6;
        ctx.drawImage(set.main, xx - dd / 2, yy - dd / 2, dd, dd);
      }
    }

    ctx.globalAlpha = 1;
    ctx.globalCompositeOperation = "source-over";
  }

  /* -------- loop ---------------------------------------------------------
     The scripted reveal (particles → compass → sweep → titles) only ever
     plays once — everything driving it is gated to t < ~4.85s in render().
     Past that the loop keeps running purely to animate the ambient sparkle
     around the finished mark, forever, so the hero background stays alive
     instead of settling into a static frame. skip() is what actually stops
     it (jumps straight to the fully static "finished" state). ------------- */
  function loop(ts) {
    if (!startTs) startTs = ts;
    clock = (ts - startTs) / 1000;
    render(clock);
    if (running) {
      raf = requestAnimationFrame(loop);
    } else {
      ctx.clearRect(0, 0, W, H);
    }
  }

  function finishEvent(homeDelayMs) {
    if (done) return;
    done = true;
    stage.classList.add("done");
    window.dispatchEvent(new CustomEvent("gotg:introComplete"));
    goHome(homeDelayMs);
  }

  /* -------- controls ------------------------------------------------- */
  function play() {
    clearTimeout(endTimer);
    cancelAnimationFrame(raf);
    done = false;
    stage.classList.remove("play", "done", "finished");
    void stage.offsetWidth;                    // reflow so CSS animations restart
    measure();
    build();

    if (reduced) {
      stage.classList.add("reduced");
      finishEvent(0);                          // nothing to watch, hand off right away
      return;
    }
    startTs = 0; clock = 0; running = true;
    stage.classList.add("play");
    raf = requestAnimationFrame(loop);
    // brief hold on the finished mark before handing off to the real homepage
    endTimer = setTimeout(function () { finishEvent(1200); }, DURATION * 1000);
  }

  function skip() {
    clearTimeout(endTimer);
    cancelAnimationFrame(raf);
    running = false;
    ctx.clearRect(0, 0, W, H);
    stage.classList.remove("play");
    stage.classList.add("finished");
    finishEvent(150);                          // skip means "get me there now"
  }

  btnRep.addEventListener("click", play);
  btnSkip.addEventListener("click", skip);

  if (btnTheme) btnTheme.addEventListener("click", function () {
    var next = theme === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", next);
    try { localStorage.setItem("gotg-intro-theme", next); } catch (e) {}
    readTheme();
    if (!running) render(clock);   // repaint a frozen final frame
  });

  /* follow theme changes (OS toggle or <html data-theme> flips) --------- */
  var mq = window.matchMedia("(prefers-color-scheme: dark)");
  if (mq.addEventListener) mq.addEventListener("change", readTheme);
  else if (mq.addListener) mq.addListener(readTheme);
  new MutationObserver(readTheme).observe(document.documentElement, {
    attributes: true, attributeFilter: ["data-theme"]
  });

  var rt;
  window.addEventListener("resize", function () {
    clearTimeout(rt);
    rt = setTimeout(function () {
      measure();
      if (!running) return;
      // keep home positions sensible after a resize mid-play
      for (var i = 0; i < particles.length; i++) {
        var p = particles[i];
        if (clock >= 2) { p.hx = cx + p.tx * logoR; p.hy = cy + p.ty * logoR; }
      }
    }, 150);
  });

  /* re-measure once everything (fonts / stylesheet) has settled */
  window.addEventListener("load", function () {
    measure();
    if (running && clock < 0.15) build();
  });

  /* kick off */
  if (document.readyState === "complete" || document.readyState === "interactive") {
    play();
  } else {
    window.addEventListener("DOMContentLoaded", play);
  }
})();
