"""Arma el PDF de evidencia del recorrido funcional de Patio El Olvidado."""

from pathlib import Path

from fpdf import FPDF
from PIL import Image

ROOT = Path(__file__).resolve().parent
SRC = Path(r"C:\Users\sebam\AppData\Local\Temp\cursor\screenshots")
OUT = ROOT / "Patio-El-Olvidado-flujos.pdf"
CAPTIONS = ROOT / "capturas"

SECTIONS = [
    (
        "1. Ingreso",
        "El sistema exige sesión (RN-01). Con el administrador de desarrollo se entra al panel y, desde ahí, a cada módulo.",
        [
            ("02-login.png", "Pantalla de inicio de sesión."),
            ("03-dashboard-admin.png", "Panel del administrador, con todos los módulos del alcance."),
            ("25-olvide-contrasena.png", "Recuperación de contraseña, sin enviar el correo en esta prueba."),
        ],
    ),
    (
        "2. Menú",
        "El administrador ve el catálogo y da de alta un producto. En esta prueba se creó Flan casero, categoría Postres, a $ 3.500.",
        [
            ("04-menu.png", "Catálogo previo: empanada, limonada y milanesa."),
            ("05-menu-alta.png", "Alta confirmada del flan casero."),
        ],
    ),
    (
        "3. Clientes y fidelización",
        "Walk-in Mostrador tenía 4 visitas. La quinta compra, contada al crear el pedido, aplica 10 % (RN-05). El historial del personal muestra pedidos anteriores ya descontados que seguían en preparación.",
        [
            ("06-clientes.png", "Listado de clientes y visitas acumuladas."),
            ("07-historial-cliente.png", "Historial de Walk-in Mostrador, con descuento de $ 240 sobre $ 2.400."),
        ],
    ),
    (
        "4. Pedido, cobro y caja",
        "Se cargó un pedido local de una milanesa napolitana para Walk-in Mostrador. Subtotal $ 8.500, descuento $ 850, total $ 7.650. Se marcó Listo y se cobró en efectivo. La caja del 26/09/2026 (UTC) quedó en $ 7.650 en efectivo.",
        [
            ("08-pedidos.png", "Bandeja de pedidos antes del caso nuevo."),
            ("09-pedido-descuento.png", "Alta con el aviso de RN-05 y el total estimado."),
            ("10-pedido-creado.png", "Pedido 15 creado, en preparación, con el descuento aplicado."),
            ("11-cobro.png", "Cobro del pedido 15: saldo $ 7.650, método efectivo."),
            ("12-pago-registrado.png", "Pago 8 completado. El pedido quedó saldado."),
            ("13-caja.png", "Caja del día: el efectivo refleja ese cobro."),
        ],
    ),
    (
        "5. Reservas",
        "La agenda del 26/09/2026 estaba vacía. Se confirmó la mesa 1 para Walk-in Mostrador, de 20:00 a 21:30, 2 personas (RN-06).",
        [
            ("14-reservas.png", "Agenda del día, sin turnos."),
            ("15-reserva-confirmada.png", "Reserva confirmada en la mesa 1."),
        ],
    ),
    (
        "6. Empleados, usuarios y fichaje",
        "El administrador ve empleados y cuentas. El empleado solo ficha sus propias horas (RN-07). En la prueba se registró una entrada y, enseguida, la salida, para no dejar un turno abierto.",
        [
            ("16-empleados.png", "Empleado Dev, mozo, tarifa $ 2.500 por hora."),
            ("17-usuarios.png", "Las tres cuentas de desarrollo: Admin, Cliente y Empleado."),
            ("28-dashboard-empleado.png", "Panel del empleado: consulta y operación de salón, sin usuarios ni reportes."),
            ("26-mis-horas.png", "Saldo, fichajes previos y una liquidación ya generada."),
            ("27-fichaje-abierto.png", "Entrada registrada el 26/09/2026. Después se cerró con la salida."),
        ],
    ),
    (
        "7. Inventario, proveedor y aviso",
        "Se creó Harina 000 con 10 kg y mínimo 5. Una salida de 6 kg por producción dejó el saldo en 4 kg y el ítem en alerta. En la misma operación se generó el aviso para el administrador (RN-12). También se dio de alta el proveedor Molino del Sur.",
        [
            ("18-inventario-alerta.png", "Harina 000 en alerta: saldo 4 kg, mínimo 5."),
            ("19-notificaciones.png", "Aviso no leído: Harina 000 quedó con saldo 4 kg."),
            ("20-notificacion-leida.png", "El mismo aviso, marcado como leído."),
            ("21-proveedores.png", "Alta de Molino del Sur."),
        ],
    ),
    (
        "8. Promociones e historia",
        "La promoción es un aviso con vigencia y no cambia el total del pedido (RN-13). Se creó Menú del mediodía, vigente del 26/09/2026 al 31/10/2026. La historia es un texto único que el administrador puede editar (RN-14).",
        [
            ("22-promociones.png", "Promoción creada, activa, en la grilla del administrador."),
            ("23-historia.png", "Texto institucional y formulario de edición, solo visible para el administrador."),
        ],
    ),
    (
        "9. Reportes",
        "El reporte de ventas del 01/09/2026 al 26/09/2026 incluye el cobro de la prueba: el 26/09 aparece $ 7.650 en efectivo, 1 pago.",
        [
            ("24-reportes.png", "Ventas por rango. El día de la prueba suma el pedido 15."),
        ],
    ),
    (
        "10. Vista del cliente",
        "El cliente autenticado ve menú, historia, promociones vigentes, sus pedidos, reservas y su historial. No administra catálogos ni caja.",
        [
            ("29-dashboard-cliente.png", "Panel del cliente, con los módulos de su rol."),
            ("30-promociones-cliente.png", "La promoción del mediodía, en solo lectura."),
            ("31-mi-historial.png", "Historial propio de Cliente Dev: un pedido local, todavía sin descuento de fidelización."),
        ],
    ),
]


class Manual(FPDF):
    def footer(self):
        self.set_y(-12)
        self.set_font("Arial", "", 9)
        self.set_text_color(90, 90, 90)
        self.cell(0, 8, f"Patio El Olvidado  ·  evidencia funcional  ·  {self.page_no()}", align="C")


def write(pdf: FPDF, text: str, size: int, style: str = "", height: float = 6):
    pdf.set_x(pdf.l_margin)
    pdf.set_font("Arial", style, size)
    pdf.multi_cell(0, height, text, align="L", new_x="LMARGIN", new_y="NEXT")


def image_size(path: Path, max_w: float = 180, max_h: float = 210):
    with Image.open(path) as img:
        w_px, h_px = img.size
    ratio = h_px / w_px
    width = max_w
    height = width * ratio
    if height > max_h:
        height = max_h
        width = height / ratio
    return width, height


def fit_image(pdf: FPDF, path: Path):
    width, height = image_size(path)
    x = (210 - width) / 2
    if pdf.get_y() + height > 280:
        pdf.add_page()
    pdf.image(str(path), x=x, w=width, h=height)


def main():
    CAPTIONS.mkdir(exist_ok=True)
    pdf = Manual(format="A4")
    pdf.set_auto_page_break(auto=True, margin=16)
    pdf.add_font("Arial", "", r"C:\Windows\Fonts\arial.ttf")
    pdf.add_font("Arial", "B", r"C:\Windows\Fonts\arialbd.ttf")
    pdf.add_font("Arial", "I", r"C:\Windows\Fonts\ariali.ttf")

    pdf.add_page()
    pdf.ln(28)
    write(pdf, "Patio El Olvidado", 22, "B", 12)
    write(pdf, "Recorrido funcional y evidencia de los módulos", 14, "", 8)
    pdf.ln(6)
    write(
        pdf,
        "Fecha de la prueba: 26 de septiembre de 2026.\n"
        "Ambiente: API local en http://localhost:5186 y front en http://localhost:5173, "
        "contra la base de desarrollo.\n"
        "Cuentas usadas: admin@patioelolvidado.local, empleado@patioelolvidado.local "
        "y cliente@patioelolvidado.local.",
        11,
    )
    pdf.ln(4)
    write(pdf, "Flujo que se recorrió", 13, "B", 8)
    write(
        pdf,
        "Autenticación y panel según el rol.\n"
        "Menú, alta de producto.\n"
        "Cliente con 4 visitas, pedido con 10 % de descuento, cobro en efectivo y caja del día.\n"
        "Reserva de mesa sin solapamiento.\n"
        "Empleados, usuarios y fichaje propio.\n"
        "Stock que cruza el mínimo, aviso in-app y proveedor.\n"
        "Promoción vigente e historia del restaurante.\n"
        "Reporte de ventas del rango, y la misma promoción vista por el cliente.",
        11,
    )
    pdf.ln(4)
    write(
        pdf,
        "Los datos quedaron en la base local de desarrollo: flan, pedido 15, reserva de la mesa 1, "
        "harina en alerta, proveedor Molino del Sur y la promoción del mediodía.",
        10,
        "I",
        5,
    )

    for title, intro, shots in SECTIONS:
        pdf.add_page()
        write(pdf, title, 16, "B", 9)
        pdf.ln(1)
        write(pdf, intro, 11)
        pdf.ln(3)
        for filename, caption in shots:
            src = SRC / filename
            if not src.exists():
                raise FileNotFoundError(src)
            dest = CAPTIONS / filename
            dest.write_bytes(src.read_bytes())
            _, height = image_size(dest)
            if pdf.get_y() + 10 + height > 275:
                pdf.add_page()
            write(pdf, caption, 11, "B")
            pdf.ln(1)
            fit_image(pdf, dest)
            pdf.ln(4)

    pdf.output(str(OUT))
    print(OUT)


if __name__ == "__main__":
    main()
