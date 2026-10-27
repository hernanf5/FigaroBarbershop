const { createApp, ref, reactive, computed, watch, onMounted } = Vue;

createApp({
    setup() {
        const token = window.FIGARO.token;
        const esAdmin = window.FIGARO.esAdmin;

        // ---------- Listado ----------
        const filtro = reactive({ q: '', estado: '', desde: '', hasta: '', idBarbero: '' });
        const datos = reactive({ items: [], total: 0, pagina: 1, totalPaginas: 1 });
        const cargando = ref(false);
        const error = ref('');
        const mensaje = ref('');
        const barberoQ = ref('');
        const barberos = ref([]);

        async function api(url, opciones = {}) {
            const r = await fetch(url, {
                ...opciones,
                headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' }
            });
            const data = await r.json().catch(() => ({}));
            if (!r.ok) throw new Error(data.error || data.title || `Error ${r.status}`);
            return data;
        }

        async function cargar(pagina = 1) {
            cargando.value = true;
            error.value = '';
            try {
                const p = new URLSearchParams({ pagina, tamPagina: 10 });
                for (const [k, v] of Object.entries(filtro)) if (v !== '') p.set(k, v);
                const data = await api('/api/turnos?' + p.toString());
                datos.items = data.items;
                datos.total = data.total;
                datos.pagina = data.pagina;
                datos.totalPaginas = data.totalPaginas;
            } catch (e) {
                error.value = e.message;
            } finally {
                cargando.value = false;
            }
        }

        let timerBusqueda;
        function buscarConDemora() {
            clearTimeout(timerBusqueda);
            timerBusqueda = setTimeout(() => cargar(1), 350);
        }

        let timerBarbero;
        function buscarBarberos() {
            clearTimeout(timerBarbero);
            if (!barberoQ.value.trim()) {
                filtro.idBarbero = '';
                barberos.value = [];
                cargar(1);
                return;
            }
            timerBarbero = setTimeout(async () => {
                try { barberos.value = await api('/api/turnos/barberos?q=' + encodeURIComponent(barberoQ.value)); }
                catch (e) { error.value = e.message; }
            }, 300);
        }

        function elegirBarbero(b) {
            filtro.idBarbero = b.idUsuario;
            barberoQ.value = b.nombre;
            barberos.value = [];
            cargar(1);
        }

        async function cambiarEstado(turno, estado) {
            if (!confirm(`¿Marcar el turno de ${turno.cliente} como "${estado}"?`)) return;
            mensaje.value = '';
            error.value = '';
            try {
                await api(`/api/turnos/${turno.idTurno}/estado`, {
                    method: 'PUT',
                    body: JSON.stringify({ estado })
                });
                mensaje.value = `Turno marcado como "${estado}".`;
                await cargar(datos.pagina);
            } catch (e) {
                error.value = e.message;
            }
        }

        // ---------- Alta ----------
        const hoy = (() => {
            const d = new Date();
            return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        })();

        const vacio = () => ({
            abierto: true, email: '', nombre: '', apellido: '', telefono: '',
            idServicio: '', fecha: '', idAgenda: '',
            idBarbero: esAdmin ? '' : window.FIGARO.idUsuario,
            barberoNombre: esAdmin ? '' : window.FIGARO.nombreUsuario
        });
        const nuevo = reactive({ ...vacio(), abierto: false });
        const servicios = ref([]);
        const clientesSug = ref([]);
        const barberosForm = ref([]);
        const horarios = ref(null);
        const guardando = ref(false);
        const errorForm = ref('');

        const puedeGuardar = computed(() =>
            nuevo.idAgenda && nuevo.idServicio && nuevo.email.trim() &&
            nuevo.nombre.trim() && nuevo.apellido.trim() && nuevo.telefono.trim());

        async function abrirNuevo() {
            Object.assign(nuevo, vacio());
            errorForm.value = '';
            horarios.value = null;
            clientesSug.value = [];
            barberosForm.value = [];
            if (!servicios.value.length) {
                try { servicios.value = await api('/api/turnos/servicios'); }
                catch (e) { errorForm.value = e.message; }
            }
        }

        function cerrarNuevo() { nuevo.abierto = false; }

        let timerCliente;
        function buscarClientes() {
            clearTimeout(timerCliente);
            const q = nuevo.email.trim();
            if (q.length < 2) { clientesSug.value = []; return; }
            timerCliente = setTimeout(async () => {
                try { clientesSug.value = await api('/api/turnos/clientes?q=' + encodeURIComponent(q)); }
                catch { clientesSug.value = []; }
            }, 300);
        }

        function elegirCliente(c) {
            nuevo.email = c.email;
            nuevo.nombre = c.nombre;
            nuevo.apellido = c.apellido;
            nuevo.telefono = c.telefono;
            clientesSug.value = [];
        }

        let timerBarberoForm;
        function buscarBarberosForm() {
            nuevo.idBarbero = '';   // si edita el texto, hay que volver a elegir
            clearTimeout(timerBarberoForm);
            const q = nuevo.barberoNombre.trim();
            if (!q) { barberosForm.value = []; return; }
            timerBarberoForm = setTimeout(async () => {
                try { barberosForm.value = await api('/api/turnos/barberos?q=' + encodeURIComponent(q)); }
                catch { barberosForm.value = []; }
            }, 300);
        }

        function elegirBarberoForm(b) {
            nuevo.idBarbero = b.idUsuario;
            nuevo.barberoNombre = b.nombre;
            barberosForm.value = [];
        }

        // Recalcula los horarios cuando cambia barbero, servicio o fecha
        async function cargarHorarios() {
            nuevo.idAgenda = '';
            horarios.value = null;
            if (!nuevo.idBarbero || !nuevo.idServicio || !nuevo.fecha) return;
            const clave = `${nuevo.idBarbero}|${nuevo.idServicio}|${nuevo.fecha}`;
            try {
                const data = await api(`/api/turnos/horarios?idBarbero=${nuevo.idBarbero}&fecha=${nuevo.fecha}&idServicio=${nuevo.idServicio}`);
                if (clave === `${nuevo.idBarbero}|${nuevo.idServicio}|${nuevo.fecha}`) horarios.value = data; // descarta respuestas viejas
            } catch (e) {
                horarios.value = [];
                errorForm.value = e.message;
            }
        }
        watch(() => [nuevo.idBarbero, nuevo.idServicio, nuevo.fecha], cargarHorarios);

        async function guardar() {
            guardando.value = true;
            errorForm.value = '';
            try {
                await api('/api/turnos', {
                    method: 'POST',
                    body: JSON.stringify({
                        idAgenda: Number(nuevo.idAgenda),
                        idServicio: Number(nuevo.idServicio),
                        email: nuevo.email.trim(),
                        nombre: nuevo.nombre.trim(),
                        apellido: nuevo.apellido.trim(),
                        telefono: nuevo.telefono.trim()
                    })
                });
                cerrarNuevo();
                mensaje.value = 'Turno creado correctamente.';
                await cargar(1);
            } catch (e) {
                errorForm.value = e.message;
                cargarHorarios(); // por si el horario se ocupó mientras tanto
            } finally {
                guardando.value = false;
            }
        }

        // ---------- Helpers ----------
        const fecha = f => { const [y, m, d] = f.slice(0, 10).split('-'); return `${d}/${m}/${y}`; };
        const dinero = n => '$' + Number(n).toLocaleString('es-AR');
        const colorEstado = e => ({
            'Confirmado': 'bg-blue-100 text-blue-800',
            'Finalizado': 'bg-green-100 text-green-800',
            'Cancelado': 'bg-red-100 text-red-800',
            'No se presentó': 'bg-amber-100 text-amber-800'
        }[e] || 'bg-gray-100 text-gray-800');

        onMounted(() => cargar(1));

        return {
            esAdmin, filtro, datos, cargando, error, mensaje, barberoQ, barberos,
            cargar, buscarConDemora, buscarBarberos, elegirBarbero, cambiarEstado, fecha, dinero, colorEstado,
            hoy, nuevo, servicios, clientesSug, barberosForm, horarios, guardando, errorForm, puedeGuardar,
            abrirNuevo, cerrarNuevo, buscarClientes, elegirCliente, buscarBarberosForm, elegirBarberoForm, guardar
        };
    }
}).mount('#app');