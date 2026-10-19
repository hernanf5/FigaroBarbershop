(() => {
    const $ = id => document.getElementById(id);
    const MESES = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];
    const TITULOS = ['', 'Elegí tu barbero', 'Elegí el servicio', 'Elegí día y horario', 'Tus datos'];
    const SENA = 5000;

    const esc = s => String(s ?? '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const money = n => '$' + Number(n).toLocaleString('es-AR');
    const pad = n => String(n).padStart(2, '0');
    const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
    const getJson = async url => {
        const r = await fetch(url);
        if (!r.ok) throw new Error('Error de red');
        return r.json();
    };

    const hoy = new Date(); hoy.setHours(0, 0, 0, 0);
    const state = {
        step: 1, barberos: null, servicios: null,
        barbero: null, servicio: null,
        mes: new Date(hoy.getFullYear(), hoy.getMonth(), 1),
        fecha: null, horarios: null, slot: null,
        email: '', nombre: '', apellido: '', telefono: '',
        clienteExiste: false, telPista: '',
        enviando: false, listo: null
    };

    const setError = msg => {
        const e = $('modal-error');
        e.textContent = msg || '';
        e.style.display = msg ? 'block' : 'none';
    };

    //Pasos 
    async function stepBarbero(body) {
        if (!state.barberos) {
            body.innerHTML = '<p class="booking-service-desc">Cargando...</p>';
            try { state.barberos = await getJson('/Reservar/Barberos'); }
            catch { body.innerHTML = '<p>No se pudieron cargar los barberos.</p>'; return; }
        }
        if (!state.barberos.length) { body.innerHTML = '<p>No hay barberos disponibles.</p>'; return; }
        body.innerHTML = '<div class="barbers-grid">' + state.barberos.map(b => `
            <div class="booking-barber ${state.barbero?.idUsuario === b.idUsuario ? 'sel' : ''}" data-id="${b.idUsuario}">
                <div class="booking-barber-photo">
                    ${b.avatarUrl ? `<img src="${esc(b.avatarUrl)}" alt="" style="width:100%;height:100%;object-fit:cover">` : '<div class="ph"></div>'}
                    ${state.barbero?.idUsuario === b.idUsuario ? '<div class="booking-barber-check">✓</div>' : ''}
                </div>
                <div class="booking-barber-info">
                    <div class="booking-barber-name">${esc(b.nombre)}</div>
                    <div class="booking-barber-tag">Barbero</div>
                </div>
            </div>`).join('') + '</div>';
        body.querySelectorAll('.booking-barber').forEach(el => el.onclick = () => {
            const nuevo = state.barberos.find(b => b.idUsuario == el.dataset.id);
            if (state.barbero?.idUsuario !== nuevo.idUsuario) resetHorario();
            state.barbero = nuevo;
            render();
        });
    }

    async function stepServicio(body) {
        if (!state.servicios) {
            body.innerHTML = '<p class="booking-service-desc">Cargando...</p>';
            try { state.servicios = await getJson('/Reservar/Servicios'); }
            catch { body.innerHTML = '<p>No se pudieron cargar los servicios.</p>'; return; }
        }
        body.innerHTML = '<div class="services-list">' + state.servicios.map(s => `
            <div class="booking-service ${state.servicio?.idServicio === s.idServicio ? 'sel' : ''}" data-id="${s.idServicio}">
                <div>
                    <div class="booking-service-name">${esc(s.nombre)}</div>
                    <div class="booking-service-desc">Seña: ${money(s.sena)}</div>
                </div>
                <div class="booking-service-meta">
                    <span class="booking-service-price">${money(s.precio)}</span>
                    <span class="booking-service-time">${s.duracionMinutos} MIN</span>
                </div>
            </div>`).join('') + '</div>';
        body.querySelectorAll('.booking-service').forEach(el => el.onclick = () => {
            const nuevo = state.servicios.find(s => s.idServicio == el.dataset.id);
            if (state.servicio?.idServicio !== nuevo.idServicio) resetHorario();
            state.servicio = nuevo;
            render();
        });
    }

    function stepHorario(body) {
        const y = state.mes.getFullYear(), m = state.mes.getMonth();
        const primero = new Date(y, m, 1);
        const offset = (primero.getDay() + 6) % 7; // semana arranca en lunes
        const diasMes = new Date(y, m + 1, 0).getDate();
        const maxMes = new Date(hoy.getFullYear(), hoy.getMonth() + 2, 1);

        let celdas = '';
        for (let i = 0; i < offset; i++) celdas += '<div class="cal-day empty"></div>';
        for (let d = 1; d <= diasMes; d++) {
            const f = new Date(y, m, d);
            const cerrado = f < hoy || f.getDay() === 0;
            const cls = ['cal-day', cerrado ? 'closed' : '', f.getTime() === hoy.getTime() ? 'today' : '',
                state.fecha === iso(f) ? 'sel' : ''].join(' ');
            celdas += `<div class="${cls}" data-fecha="${iso(f)}">${d}</div>`;
        }

        body.innerHTML = `
            <div class="calendar-wrap">
                <div class="cal-header">
                    <button type="button" class="cal-nav" id="cal-prev">‹</button>
                    <span class="cal-title">${MESES[m]} ${y}</span>
                    <button type="button" class="cal-nav" id="cal-next">›</button>
                </div>
                <div class="cal-days-header">${['L','M','M','J','V','S','D'].map(x => `<span>${x}</span>`).join('')}</div>
                <div class="cal-days">${celdas}</div>
            </div>
            <div id="slots-zone"></div>`;

        $('cal-prev').onclick = () => {
            const prev = new Date(y, m - 1, 1);
            if (prev >= new Date(hoy.getFullYear(), hoy.getMonth(), 1)) { state.mes = prev; render(); }
        };
        $('cal-next').onclick = () => {
            const next = new Date(y, m + 1, 1);
            if (next < maxMes) { state.mes = next; render(); }
        };
        body.querySelectorAll('.cal-day:not(.empty):not(.closed)').forEach(el => el.onclick = () => {
            state.fecha = el.dataset.fecha; state.slot = null; state.horarios = null;
            cargarHorarios();
            body.querySelectorAll('.cal-day').forEach(x => x.classList.toggle('sel', x === el));
            renderSlots();
            updateFooter();
        });
        renderSlots();
    }

    async function cargarHorarios() {
        const fecha = state.fecha;
        const url = `/Reservar/Horarios?idBarbero=${state.barbero.idUsuario}&fecha=${fecha}&idServicio=${state.servicio.idServicio}`;
        try {
            const data = await getJson(url);
            if (state.fecha !== fecha) return; // respuesta vieja
            state.horarios = data;
        } catch {
            if (state.fecha !== fecha) return;
            state.horarios = [];
            setError('No se pudieron cargar los horarios.');
        }
        renderSlots();
    }

    function renderSlots() {
        const z = $('slots-zone');
        if (!z) return;
        if (!state.fecha) { z.innerHTML = ''; return; }
        if (state.horarios === null) { z.innerHTML = '<p class="booking-service-desc" style="margin-top:20px">Buscando horarios...</p>'; return; }
        if (!state.horarios.length) { z.innerHTML = '<p class="booking-service-desc" style="margin-top:20px">No hay horarios disponibles ese día.</p>'; return; }
        z.innerHTML = '<div class="slots-header">Horarios disponibles</div><div class="slots-grid">' +
            state.horarios.map(h => `<div class="slot ${state.slot?.idAgenda === h.idAgenda ? 'sel' : ''}" data-id="${h.idAgenda}">${esc(h.hora)}</div>`).join('') + '</div>';
        z.querySelectorAll('.slot').forEach(el => el.onclick = () => {
            state.slot = state.horarios.find(h => h.idAgenda == el.dataset.id);
            z.querySelectorAll('.slot').forEach(x => x.classList.toggle('sel', x === el));
            updateFooter();
        });
    }

    function stepContacto(body) {
        const telPlaceholder = state.clienteExiste
            ? `Tenemos uno que termina en ${esc(state.telPista)} (opcional)`
            : '11 1234 5678';
        body.innerHTML = `
            <div class="form-grid">
                <div class="form-field full">
                    <label class="form-label">Email</label>
                    <input class="form-input" id="f-email" type="email" placeholder="tu@email.com" value="${esc(state.email)}" autocomplete="email">
                    <span id="f-hint" class="booking-service-desc">${state.clienteExiste ? 'Te encontramos, completamos tus datos. Podés cambiar lo que quieras.' : ''}</span>
                </div>
                <div class="form-field">
                    <label class="form-label">Nombre</label>
                    <input class="form-input" id="f-nombre" value="${esc(state.nombre)}">
                </div>
                <div class="form-field">
                    <label class="form-label">Apellido</label>
                    <input class="form-input" id="f-apellido" value="${esc(state.apellido)}">
                </div>
                <div class="form-field full">
                    <label class="form-label">Teléfono</label>
                    <input class="form-input" id="f-tel" type="tel" placeholder="${telPlaceholder}" value="${esc(state.telefono)}">
                </div>
            </div>`;

        const bind = (id, key) => $(id).addEventListener('input', e => { state[key] = e.target.value; updateFooter(); });
        bind('f-nombre', 'nombre'); bind('f-apellido', 'apellido'); bind('f-tel', 'telefono');

        let t;
        $('f-email').addEventListener('input', e => {
            state.email = e.target.value.trim();
            updateFooter();
            clearTimeout(t);
            t = setTimeout(buscarCliente, 400);
        });
    }

    async function buscarCliente() {
        const email = state.email;
        if (!/^\S+@\S+\.\S+$/.test(email)) return;
        try {
            const r = await getJson('/Reservar/BuscarCliente?email=' + encodeURIComponent(email));
            if (state.email !== email) return;
            state.clienteExiste = r.existe;
            if (r.existe) {
                state.nombre = r.nombre; state.apellido = r.apellido; state.telPista = r.telefonoTermina;
                state.telefono = '';
            }
            $('f-nombre').value = state.nombre; $('f-apellido').value = state.apellido;
            $('f-tel').placeholder = r.existe ? `Tenemos uno que termina en ${r.telefonoTermina} (opcional)` : '11 1234 5678';
            $('f-hint').textContent = r.existe ? 'Te encontramos, completamos tus datos. Podés cambiar lo que quieras.' : '';
            updateFooter();
        } catch { /* sin autocompletar, el usuario sigue a mano */ }
    }

    function stepListo(body) {
        const r = state.listo;
        body.innerHTML = `
            <div style="text-align:center; padding:40px 0;">
                <div class="booking-barber-check" style="position:static; margin:0 auto 24px; width:48px; height:48px; font-size:22px;">✓</div>
                <h3 style="font-family:var(--serif); font-size:32px; font-weight:300; margin-bottom:12px;">¡Turno reservado!</h3>
                <p class="booking-service-desc" style="margin-bottom:24px;">Reserva #${r.idTurno} con ${esc(state.barbero.nombre)}</p>
                <div class="booking-summary" style="text-align:left;">
                    <div class="bsum-item"><div class="bsum-label">Servicio</div><div class="bsum-value">${esc(state.servicio.nombre)}</div></div>
                    <div class="bsum-item"><div class="bsum-label">Cuándo</div><div class="bsum-value">${fechaLinda(state.fecha)} · ${esc(state.slot.hora)}</div></div>
                    <div class="bsum-item"><div class="bsum-label">Seña a abonar</div><div class="bsum-value">${money(r.sena)}</div></div>
                </div>
                <p class="booking-service-desc">Te esperamos. Recordá abonar la seña para asegurar el turno.</p>
            </div>`;
    }

    // ---------- Helpers ----------
    function fechaLinda(f) {
        const [y, m, d] = f.split('-').map(Number);
        return `${d} de ${MESES[m - 1]}`;
    }
    function resetHorario() { state.fecha = null; state.horarios = null; state.slot = null; }

    function puedeAvanzar() {
        switch (state.step) {
            case 1: return !!state.barbero;
            case 2: return !!state.servicio;
            case 3: return !!state.slot;
            case 4:
                return /^\S+@\S+\.\S+$/.test(state.email) && state.nombre.trim() && state.apellido.trim()
                    && (state.clienteExiste || state.telefono.trim());
        }
        return false;
    }

    function renderSummary() {
        const items = [];
        if (state.barbero) items.push(['Barbero', state.barbero.nombre]);
        if (state.servicio) items.push(['Servicio', state.servicio.nombre], ['Precio', money(state.servicio.precio)]);
        if (state.fecha && state.slot) items.push(['Fecha', fechaLinda(state.fecha)], ['Hora', state.slot.hora]);
        $('modal-summary').innerHTML = items.map(([k, v]) =>
            `<div class="modal-summary-item"><span>${k}</span><span>${esc(v)}</span></div>`).join('');
    }

    function updateFooter() {
        const final = state.step === 5;
        $('btn-back').style.display = (state.step === 1 || final) ? 'none' : '';
        $('btn-next').style.display = final ? 'none' : '';
        $('btn-next').textContent = state.step === 4 ? 'Confirmar reserva' : 'Continuar';
        $('btn-next').disabled = state.enviando || !puedeAvanzar();
        $('btn-next').style.opacity = $('btn-next').disabled ? '.4' : '1';
        $('modal-hint').textContent = state.step === 4 ? `Seña de ${money(state.servicio ? state.servicio.sena : SENA)} para confirmar` : '';
        renderSummary();
    }

    async function render() {
        setError('');
        for (let i = 1; i <= 4; i++) {
            const el = $('step-' + i);
            el.classList.toggle('active', state.step === i);
            el.classList.toggle('done', state.step > i);
        }
        $('modal-title').textContent = state.step === 5 ? 'Listo' : TITULOS[state.step];
        const body = $('modal-body');
        updateFooter();
        const paso = state.step;
        if (paso === 1) await stepBarbero(body);
        else if (paso === 2) await stepServicio(body);
        else if (paso === 3) stepHorario(body);
        else if (paso === 4) stepContacto(body);
        else stepListo(body);
        updateFooter();
    }

    async function confirmar() {
        state.enviando = true; updateFooter(); setError('');
        try {
            const r = await fetch('/Reservar/Confirmar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    idAgenda: state.slot.idAgenda,
                    idServicio: state.servicio.idServicio,
                    email: state.email.trim(),
                    nombre: state.nombre.trim(),
                    apellido: state.apellido.trim(),
                    telefono: state.telefono.trim() || null
                })
            });
            const data = await r.json().catch(() => ({}));
            if (r.ok) { state.listo = data; state.step = 5; }
            else if (r.status === 409) {
                resetHorario(); state.step = 3; state.enviando = false;
                await render();
                setError(data.error || 'Ese horario se acaba de ocupar. Elegí otro.');
                return;
            } else setError(data.error || 'No se pudo confirmar la reserva.');
        } catch { setError('Error de conexión. Probá de nuevo.'); }
        state.enviando = false;
        render();
    }

    $('btn-back').onclick = () => { if (state.step > 1) { state.step--; render(); } };
    $('btn-next').onclick = () => {
        if (!puedeAvanzar()) return;
        if (state.step === 4) confirmar();
        else { state.step++; render(); }
    };

    render();
})();