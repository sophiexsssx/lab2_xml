#!/usr/bin/env python3
"""Генерує UML-діаграми лабораторної №2: один опис сцени -> lab2_uml.drawio (3 сторінки) + SVG.

Запуск: python3 lab2_uml/generate_uml.py lab2_uml
Потрібен Pillow (для вимірювання ширини тексту).
"""
import math, os, sys
from xml.sax.saxutils import escape
from PIL import ImageFont

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
os.makedirs(OUT, exist_ok=True)
FONT = "/System/Library/Fonts/Supplemental/Arial.ttf"
_fonts = {}
warnings = []


def tw(s, size=12):
    f = _fonts.setdefault(size, ImageFont.truetype(FONT, size))
    return f.getlength(s)


def q(s):
    return escape(s, {'"': "&quot;"})


INK = "#222222"
FILL = "#ffffff"
FILLS = {"boundary": "#eaf2fb", "control": "#fdf3e1", "entity": "#eaf6ec",
         "interface": "#f1ecfa", "strategy": "#f1ecfa", "service": "#f4f4f4", "app": "#f7f7fb"}
NOTE = "#fff9d6"
LH = 16


class Page:
    def __init__(self, pid, name, w, h):
        self.pid, self.name, self.w, self.h = pid, name, w, h
        self.svg, self.cells = [], []
        self.n = 2
        self.boxes = {}

    def nid(self):
        self.n += 1
        return f"{self.pid}{self.n}"

    # ---------- primitives ----------
    def vertex(self, x, y, w, h, value, style):
        i = self.nid()
        self.cells.append(
            f'<mxCell id="{i}" value="{q(value)}" style="{style}" vertex="1" parent="1">'
            f'<mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry"/></mxCell>')
        return i

    def svg_text(self, x, y, s, size=12, anchor="middle", bold=False, italic=False, color=INK, halo=False, underline=False):
        a = ' font-weight="bold"' if bold else ""
        a += ' font-style="italic"' if italic else ""
        a += ' text-decoration="underline"' if underline else ""
        if halo:
            a += ' paint-order="stroke" stroke="#ffffff" stroke-width="4" stroke-linejoin="round"'
        self.svg.append(f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" text-anchor="{anchor}" fill="{color}"{a}>{escape(s)}</text>')

    def text(self, x, y, s, size=12, anchor="middle", bold=False, italic=False, halo=False, w=None):
        """Вільний текст; y — вертикальний центр."""
        lines = s.split("\n")
        tot = len(lines) * (size + 3)
        for k, l in enumerate(lines):
            self.svg_text(x, y - tot / 2 + (k + .78) * (size + 3), l, size, anchor, bold, italic, halo=halo)
        width = w or (max(tw(l, size) for l in lines) + 12)
        cx = {"middle": x - width / 2, "start": x - 3, "end": x - width + 3}[anchor]
        al = {"middle": "center", "start": "left", "end": "right"}[anchor]
        st = (f"text;html=1;strokeColor=none;align={al};verticalAlign=middle;whiteSpace=nowrap;"
              f"fontSize={size};fontStyle={(1 if bold else 0) + (2 if italic else 0)};"
              + ("labelBackgroundColor=#ffffff;" if halo else "fillColor=none;"))
        self.vertex(round(cx), round(y - tot / 2 - 1), round(width), round(tot + 2), escape(s).replace("\n", "<br>"), st)

    def text_in(self, cx, cy, s, size=13, bold=False):
        lines = s.split("\n")
        tot = len(lines) * (size + 3)
        for k, l in enumerate(lines):
            self.svg_text(cx, cy - tot / 2 + (k + .78) * (size + 3), l, size, "middle", bold)

    def ellipse(self, key, cx, cy, w, h, value, fill="#eaf2fb"):
        for l in value.split("\n"):
            if tw(l, 13) > w * .84:
                warnings.append(f"ellipse text wide: {l}")
        self.svg.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{w/2}" ry="{h/2}" fill="{fill}" stroke="{INK}" stroke-width="1.2"/>')
        self.text_in(cx, cy, value, 13)
        i = self.vertex(cx - w / 2, cy - h / 2, w, h, escape(value).replace("\n", "<br>"),
                        f"ellipse;html=1;whiteSpace=wrap;fontSize=13;fillColor={fill};strokeColor={INK};")
        self.boxes[key] = dict(id=i, x=cx - w / 2, y=cy - h / 2, w=w, h=h)

    def actor_shape(self, x, y, h=60, label="Користувач"):
        r = h * .15
        self.svg.append(
            f'<g fill="none" stroke="{INK}" stroke-width="1.4"><circle cx="{x}" cy="{y+r}" r="{r}" fill="#ffffff"/>'
            f'<path d="M{x} {y+2*r}V{y+h*.65}M{x-h*.27} {y+h*.42}H{x+h*.27}M{x} {y+h*.65}L{x-h*.25} {y+h}M{x} {y+h*.65}L{x+h*.25} {y+h}"/></g>')
        self.svg_text(x, y + h + 15, label, 13)

    def package(self, x, y, w, h, name):
        tabw = tw(name, 13) + 24
        self.svg.append(f'<path d="M{x} {y}H{x+tabw}V{y+22}H{x+w}V{y+h}H{x}Z" fill="none" stroke="#9a8a92" stroke-width="1.3"/>'
                        f'<path d="M{x} {y+22}H{x+tabw}" stroke="#9a8a92" stroke-width="1.3"/>')
        self.svg_text(x + 10, y + 16, name, 13, "start", bold=True, color="#5a4650")
        self.vertex(x, y, w, h, name,
                    f"shape=folder;fontStyle=1;spacingTop=2;tabWidth={round(tabw)};tabHeight=22;tabPosition=left;html=1;"
                    "verticalAlign=top;align=left;spacingLeft=10;fontSize=13;fillColor=none;strokeColor=#9a8a92;fontColor=#5a4650;")

    def class_box(self, key, x, y, w, name, stereo=None, attrs=(), methods=(), kind="service"):
        """Рядок, що починається з '$', — статичний член (підкреслюється за нотацією UML)."""
        fill = FILLS[kind]
        hdr = (LH if stereo else 0) + LH + 8
        ah = len(attrs) * LH + (8 if attrs else 6)
        mh = len(methods) * LH + (8 if methods else 6)
        h = hdr + ah + mh
        clean = lambda l: (lambda t: "\u00a0" * (len(t) - len(t.lstrip(" "))) + t.lstrip(" "))(l[1:] if l.startswith("$") else l)
        for l in list(attrs) + list(methods):
            if tw(clean(l), 12) > w - 12:
                warnings.append(f"class {name}: too wide ({tw(clean(l),12):.0f}>{w-12}): {l}")
        s = self.svg
        s.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="{INK}" stroke-width="1.2"/>')
        yy = y + 4
        if stereo:
            self.svg_text(x + w / 2, yy + 12, f"«{stereo}»", 11, italic=True)
            yy += LH
        self.svg_text(x + w / 2, yy + 13, name, 13, bold=True, italic=kind == "interface")
        s.append(f'<path d="M{x} {y+hdr}H{x+w}M{x} {y+hdr+ah}H{x+w}" stroke="{INK}" stroke-width="1"/>')
        for k, l in enumerate(attrs):
            self.svg_text(x + 7, y + hdr + 3 + (k + .8) * LH, clean(l), 12, "start", underline=l.startswith("$"))
        for k, l in enumerate(methods):
            self.svg_text(x + 7, y + hdr + ah + 3 + (k + .8) * LH, clean(l), 12, "start",
                          italic=kind == "interface", underline=l.startswith("$"))

        ps = "margin:0;padding:0 7px;line-height:16px;font-size:12px;white-space:nowrap;"

        def row(l, ital=False):
            t = escape(clean(l))
            if l.startswith("$"): t = f"<u>{t}</u>"
            if ital: t = f"<i>{t}</i>"
            return f'<p style="{ps}">{t}</p>'

        html = '<div style="padding-top:4px;text-align:center;line-height:16px;">'
        if stereo:
            html += f'<i style="font-size:11px;">«{escape(stereo)}»</i><br>'
        nm = escape(name)
        html += f'<b style="font-size:13px;">{"<i>" + nm + "</i>" if kind == "interface" else nm}</b></div><hr style="margin:3px 0 3px 0;">'
        html += "".join(row(l) for l in attrs) or '<p style="margin:0;height:0;"></p>'
        html += '<hr style="margin:3px 0 3px 0;">'
        html += "".join(row(l, kind == "interface") for l in methods)
        i = self.vertex(x, y, w, h, html,
                        f"verticalAlign=top;align=left;overflow=fill;html=1;whiteSpace=wrap;fontSize=12;fillColor={fill};strokeColor={INK};")
        self.boxes[key] = dict(id=i, x=x, y=y, w=w, h=h)
        return self.boxes[key]

    def note(self, x, y, w, h, value, size=12):
        c = 12
        self.svg.append(f'<path d="M{x} {y}H{x+w-c}L{x+w} {y+c}V{y+h}H{x}Z" fill="{NOTE}" stroke="{INK}" stroke-width="1"/>'
                        f'<path d="M{x+w-c} {y}V{y+c}H{x+w}" fill="none" stroke="{INK}" stroke-width="1"/>')
        lines = value.split("\n")
        tot = len(lines) * (size + 3)
        if tot > h - 8:
            warnings.append(f"note too tall: {lines[0]}")
        for k, l in enumerate(lines):
            if tw(l, size) > w - 16:
                warnings.append(f"note too wide: {l}")
            self.svg_text(x + 8, y + h / 2 - tot / 2 + (k + .78) * (size + 3), l, size, "start")
        self.vertex(x, y, w, h, escape(value).replace("\n", "<br>"),
                    f"shape=note;size={c};html=1;whiteSpace=wrap;align=left;spacingLeft=6;fontSize={size};fillColor={NOTE};strokeColor={INK};")

    # ---------- edges ----------
    def edge(self, pts, kind, label=None, lpos=None, src=None, dst=None, lanchor="middle"):
        """pts — точна ламана. kind: assoc, agg, comp, real, gen, dep, line, dash, sync, ret"""
        P = pts
        dashed = kind in ("real", "dep", "ret", "dash")
        d = " ".join(("M" if k == 0 else "L") + f"{p[0]:.1f} {p[1]:.1f}" for k, p in enumerate(P))
        dash = ' stroke-dasharray="6 4"' if dashed else ""
        self.svg.append(f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="1.2"{dash}/>')
        if kind in ("assoc", "agg", "comp", "dep", "ret"):
            self._open_arrow(P[-2], P[-1])
        if kind in ("real", "gen"):
            self._tri(P[-2], P[-1], False)
        if kind == "sync":
            self._tri(P[-2], P[-1], True, 10)
        if kind in ("agg", "comp"):
            self._diamond(P[1], P[0], kind == "comp")
        st = "html=1;rounded=0;edgeStyle=none;fontSize=12;strokeColor=" + INK + ";"
        st += {"assoc": "endArrow=open;endFill=0;endSize=10;",
               "agg": "startArrow=diamondThin;startFill=0;startSize=16;endArrow=open;endFill=0;endSize=10;",
               "comp": "startArrow=diamondThin;startFill=1;startSize=16;endArrow=open;endFill=0;endSize=10;",
               "real": "dashed=1;endArrow=block;endFill=0;endSize=13;",
               "gen": "endArrow=block;endFill=0;endSize=13;",
               "dep": "dashed=1;endArrow=open;endFill=0;endSize=10;",
               "ret": "dashed=1;endArrow=open;endFill=0;endSize=9;",
               "sync": "endArrow=block;endFill=1;endSize=8;",
               "dash": "dashed=1;endArrow=none;",
               "line": "endArrow=none;"}[kind]
        attrs = ""
        geo = ""
        if src:
            b = self.boxes[src]
            attrs += f' source="{b["id"]}"'
            st += f"exitX={(P[0][0]-b['x'])/b['w']:.4f};exitY={(P[0][1]-b['y'])/b['h']:.4f};exitDx=0;exitDy=0;exitPerimeter=0;"
        else:
            geo += f'<mxPoint x="{P[0][0]:.1f}" y="{P[0][1]:.1f}" as="sourcePoint"/>'
        if dst:
            b = self.boxes[dst]
            attrs += f' target="{b["id"]}"'
            st += f"entryX={(P[-1][0]-b['x'])/b['w']:.4f};entryY={(P[-1][1]-b['y'])/b['h']:.4f};entryDx=0;entryDy=0;entryPerimeter=0;"
        else:
            geo += f'<mxPoint x="{P[-1][0]:.1f}" y="{P[-1][1]:.1f}" as="targetPoint"/>'
        if len(P) > 2:
            geo += '<Array as="points">' + "".join(f'<mxPoint x="{p[0]:.1f}" y="{p[1]:.1f}"/>' for p in P[1:-1]) + "</Array>"
        self.cells.append(f'<mxCell id="{self.nid()}" value="" style="{st}" edge="1" parent="1"{attrs}>'
                          f'<mxGeometry relative="1" as="geometry">{geo}</mxGeometry></mxCell>')
        if label:
            if lpos is None:
                a, b_ = P[len(P) // 2 - 1], P[len(P) // 2]
                lpos = ((a[0] + b_[0]) / 2, (a[1] + b_[1]) / 2)
            self.text(lpos[0], lpos[1], label, 12, lanchor, halo=True)

    def _unit(self, a, b):
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy) or 1
        return dx / L, dy / L

    def _open_arrow(self, a, b, s=10):
        ux, uy = self._unit(a, b)
        px, py = -uy, ux
        self.svg.append(f'<path d="M{b[0]-ux*s+px*s*.5:.1f} {b[1]-uy*s+py*s*.5:.1f}L{b[0]:.1f} {b[1]:.1f}L{b[0]-ux*s-px*s*.5:.1f} {b[1]-uy*s-py*s*.5:.1f}" fill="none" stroke="{INK}" stroke-width="1.2"/>')

    def _tri(self, a, b, filled, s=13):
        ux, uy = self._unit(a, b)
        px, py = -uy, ux
        f = INK if filled else "#ffffff"
        self.svg.append(f'<path d="M{b[0]:.1f} {b[1]:.1f}L{b[0]-ux*s+px*s*.5:.1f} {b[1]-uy*s+py*s*.5:.1f}L{b[0]-ux*s-px*s*.5:.1f} {b[1]-uy*s-py*s*.5:.1f}Z" fill="{f}" stroke="{INK}" stroke-width="1.2"/>')

    def _diamond(self, a, b, filled, s=16):
        ux, uy = self._unit(a, b)
        px, py = -uy, ux
        m = (b[0] - ux * s / 2, b[1] - uy * s / 2)
        f = INK if filled else "#ffffff"
        self.svg.append(f'<path d="M{b[0]:.1f} {b[1]:.1f}L{m[0]+px*5:.1f} {m[1]+py*5:.1f}L{b[0]-ux*s:.1f} {b[1]-uy*s:.1f}L{m[0]-px*5:.1f} {m[1]-py*5:.1f}Z" fill="{f}" stroke="{INK}" stroke-width="1.2"/>')

    # ---------- output ----------
    def to_svg(self):
        return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}" '
                f'font-family="Arial, Helvetica, sans-serif"><rect width="{self.w}" height="{self.h}" fill="#ffffff"/>'
                + "".join(self.svg) + "</svg>")

    def to_diagram(self):
        return (f'<diagram id="{self.pid}" name="{q(self.name)}"><mxGraphModel dx="1200" dy="900" grid="1" gridSize="10" guides="1" '
                f'tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="{self.w}" pageHeight="{self.h}" math="0" shadow="0">'
                f'<root><mxCell id="0"/><mxCell id="1" parent="0"/>' + "".join(self.cells) + "</root></mxGraphModel></diagram>")


def mid(b):
    return (b["x"] + b["w"] / 2, b["y"] + b["h"] / 2)


def bottom(b):
    return b["y"] + b["h"]


def right(b):
    return b["x"] + b["w"]


# =====================================================================
# 1. USE CASE
# =====================================================================
def use_case():
    p = Page("uc", "1 - Use Case", 1120, 870)
    BX, BY, BW, BH = 190, 20, 900, 830
    title = "XML Conference Analyzer: аналіз XML-файлів про наукові конференції"
    p.svg.append(f'<rect x="{BX}" y="{BY}" width="{BW}" height="{BH}" fill="#ffffff" stroke="{INK}" stroke-width="1.4"/>')
    p.vertex(BX, BY, BW, BH, title, f"rounded=0;html=1;verticalAlign=top;fontSize=15;fontStyle=1;fillColor=#ffffff;strokeColor={INK};")
    p.svg_text(BX + BW / 2, BY + 24, title, 15, bold=True)

    AX, AY = 75, 390
    p.actor_shape(AX, AY)
    aid = p.vertex(AX - 18, AY, 36, 60, "Користувач",
                   f"shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;fontSize=13;strokeColor={INK};fillColor=#ffffff;")
    p.boxes["actor"] = dict(id=aid, x=AX - 18, y=AY, w=36, h=60)

    W, H = 240, 58
    LX, RX = 400, 860
    base = [("open", 110, "Відкрити XML-файл"),
            ("sample", 195, "Відкрити вбудований\nприклад conferences.xml"),
            ("search", 295, "Знайти конференції"),
            ("clear", 390, "Очистити результати\nі параметри пошуку"),
            ("save", 475, "Зберегти результати\nу XML-файл"),
            ("html", 565, "Трансформувати\nрезультати в HTML"),
            ("about", 690, "Переглянути довідку\nпро програму"),
            ("exit", 785, "Завершити роботу")]
    for k, y, t in base:
        p.ellipse(k, LX, y, W, H, t)
    extra = [("crit", 263, "Задати ключове слово\nі значення атрибутів"),
             ("algo", 331, "Обрати спосіб аналізу\n(SAX / DOM / LINQ to XML)"),
             ("xsl", 565, "Обрати XSL-файл\n(вбудований або власний)"),
             ("openhtml", 635, "Відкрити збережений\nHTML-файл"),
             ("conf", 785, "Підтвердити вихід\n(«Так» / «Ні»)")]
    for k, y, t in extra:
        p.ellipse(k, RX, y, W + 10, H, t, fill="#f4f4f4")

    def on(key, tx, ty):
        b = p.boxes[key]
        cx, cy = mid(b)
        dx, dy = tx - cx, ty - cy
        t = 1 / math.sqrt((dx / (b["w"] / 2)) ** 2 + (dy / (b["h"] / 2)) ** 2)
        return (cx + dx * t, cy + dy * t)

    start = (AX + 18, AY + 30)
    for k, _, _ in base:
        p.edge([start, (p.boxes[k]["x"], mid(p.boxes[k])[1])], "line", src="actor", dst=k)

    def rel(a, b, lab, kind="dep"):
        A, B = on(a, *mid(p.boxes[b])), on(b, *mid(p.boxes[a]))
        p.edge([A, B], kind, lab, src=a, dst=b)

    rel("search", "crit", "«include»")
    rel("search", "algo", "«include»")
    rel("html", "xsl", "«include»")
    rel("openhtml", "html", "«extend»")
    rel("exit", "conf", "«include»")

    p.note(735, 392, 335, 128,
           "Пошук без критеріїв повертає всі конференції.\n"
           "Пошук без відкритого файлу показує підказку\n"
           "«Файл не відкрито».\n"
           "Зберегти й трансформувати можна поточні\n"
           "результати пошуку, тому ці дії доступні\n"
           "лише після виконання пошуку.")
    p.note(735, 680, 335, 62,
           "Значення атрибутів підвантажуються у випадні\n"
           "списки: користувач обирає лише наявні у файлі.")
    return p


# =====================================================================
# 2. CLASS
# =====================================================================
def class_diagram():
    p = Page("cl", "2 - Class", 2020, 100)
    C1, C2, C3, C4 = (40, 360), (450, 470), (1040, 460), (1570, 400)   # (x, width)
    S = "(sender: object?, e: EventArgs): void"

    # ---------------- пакет застосунку ----------------
    APP_TOP = 20
    y = APP_TOP + 50
    b = p.class_box("mp", C1[0], y, C1[1], "MauiProgram", "static", [], [
        "$+ CreateMauiApp(): MauiApp"], kind="app")
    b = p.class_box("app", C1[0], bottom(b) + 50, C1[1], "App : Application", None, [], [
        "+ App()",
        "# CreateWindow(activationState: IActivationState?): Window"], kind="app")
    b = p.class_box("shell", C1[0], bottom(b) + 50, C1[1], "AppShell : Shell", None, [], [
        "+ AppShell()"], kind="app")

    page = p.class_box("page", C2[0], y, C2[1], "MainPage : ContentPage", "boundary", [
        '$- AnyValue: string = "(будь-яке)" {const}',
        "- _viewModel: MainViewModel {readonly}",
        "- _rebuilding: bool",
        "$+ ExitConfirmed: bool {get; private set}"], [
        "+ MainPage(viewModel: MainViewModel)",
        "- OnOpenXmlClicked" + S,
        "- OnOpenSampleClicked" + S,
        "- OnSearchClicked" + S,
        "- OnClearClicked" + S,
        "- OnSaveClicked" + S,
        "- OnTransformClicked" + S,
        "- OnAboutClicked" + S,
        "- OnExitClicked" + S,
        "- OnKeywordChanged(sender: object?, e: TextChangedEventArgs): void",
        "+ RequestExitAsync(): Task",
        "- ConfirmExitAsync(): Task<bool>",
        "- LoadFileAsync(path: string, displayName: string): Task",
        "- BuildStrategyList(): void",
        "- ShowParameters(): void",
        "- ShowResults(): void",
        "- ChooseXslAsync(): Task<string?>",
        "- ShowErrorAsync(title: string, ex: Exception): Task"], kind="boundary")

    fd = p.class_box("fd", C3[0], y, C3[1], "FileDialogs", "static", [
        "$- XmlType: FilePickerFileType {readonly}",
        "$- XslType: FilePickerFileType {readonly}"], [
        "$+ PickXmlAsync(): Task<FileResult?>",
        "$+ PickXslAsync(): Task<string?>",
        "$+ ExtractAssetAsync(name: string): Task<string>",
        "$+ NewTempPath(extension: string): string",
        "$+ SaveCopyAsync(sourcePath: string, suggestedName: string,",
        "$      keepSource: bool = false): Task<string?>"], kind="app")
    rr = p.class_box("rr", C3[0], bottom(fd) + 60, C3[1], "ResultRow", "record", [
        "+ Title: string {get}",
        "+ Summary: string {get}",
        "+ Details: string {get}",
        "$- SummaryAttributes: HashSet<string> {readonly}"], [
        "+ ResultRow(Title: string, Summary: string, Details: string)",
        "$+ From(c: Conference): ResultRow"], kind="app")
    al = p.class_box("al", C4[0], rr["y"], C4[1], "AttributeLabels", "static", [
        "$- Labels: Dictionary<string, string> {readonly}"], [
        "$+ For(attribute: string): string"], kind="app")

    app_bottom = max(bottom(page), bottom(rr), bottom(p.boxes["shell"])) + 30
    p.package(20, APP_TOP, p.w - 40, app_bottom - APP_TOP, "XmlAnalyzerApp  (інтерфейс .NET MAUI)")

    # ---------------- пакет ядра ----------------
    CORE_TOP = app_bottom + 30
    y = CORE_TOP + 50
    vm = p.class_box("vm", C2[0], y, C2[1], "MainViewModel", "control", [
        "- _context: XmlSearchContext {readonly}",
        "- _catalog: AttributeCatalog {readonly}",
        "- _writer: ConferenceXmlWriter {readonly}",
        "- _transformer: HtmlTransformer {readonly}",
        "+ XmlPath: string? {get; private set}",
        "+ Strategies: IReadOnlyList<IXmlSearchStrategy> {get}",
        "+ SelectedStrategy: IXmlSearchStrategy",
        "+ AttributeValues: Dictionary<string, List<string>>",
        "+ Keyword: string",
        "+ SelectedFilters: Dictionary<string, string> {get}",
        "+ Results: List<Conference> {get; private set}",
        "+ HasSearched: bool {get; private set}",
        "+ LastCriteria: SearchCriteria? {get; private set}"], [
        "+ MainViewModel(strategies: IEnumerable<IXmlSearchStrategy>,",
        "      context: XmlSearchContext, catalog: AttributeCatalog,",
        "      writer: ConferenceXmlWriter, transformer: HtmlTransformer)",
        "$+ CreateDefault(): MainViewModel",
        "+ LoadFile(path: string): void",
        "+ Search(): void",
        "+ Clear(): void",
        "+ SaveResults(path: string): void",
        "+ TransformToHtml(xslPath: string, htmlPath: string): void",
        "- EnsureSearched(): void"], kind="control")

    ctx = p.class_box("ctx", C1[0], vm["y"] + 70, C1[1], "XmlSearchContext", None, [
        "- _strategy: IXmlSearchStrategy?"], [
        "+ SetStrategy(s: IXmlSearchStrategy): void",
        "+ Execute(path: string, c: SearchCriteria): List<Conference>"], kind="strategy")

    cat = p.class_box("cat", C3[0], y, C3[1], "AttributeCatalog", None, [], [
        "+ Load(path: string): Dictionary<string, List<string>>"])
    tr = p.class_box("tr", C3[0], bottom(cat) + 36, C3[1], "HtmlTransformer", None, [], [
        "+ Transform(xmlPath: string, xslPath: string, htmlPath: string): void"])
    wr = p.class_box("wr", C3[0], bottom(tr) + 36, C3[1], "ConferenceXmlWriter", None, [], [
        "+ Write(items: List<Conference>, path: string): void"])

    names = p.class_box("names", C4[0], y, C4[1], "XmlNames", "static", [
        '$+ Root: string = "Conferences" {const}',
        '$+ Conference: string = "Conference" {const}',
        '$+ Dates: string = "Dates" {const}',
        '$+ Start: string = "start" {const}',
        '$+ End: string = "end" {const}',
        '$+ SubmissionDeadline: string = "SubmissionDeadline" {const}',
        '$+ Title: string = "title" {const}'], [], kind="service")
    fmt = p.class_box("fmt", C4[0], bottom(names) + 36, C4[1], "XmlFormat", "static, internal", [
        "$~ ReaderSettings: XmlReaderSettings {get}"], [
        "$~ WrongRoot(actual: string?): InvalidDataException"], kind="service")

    row2 = max(bottom(vm), bottom(wr) + 40) + 70
    itf = p.class_box("itf", C2[0], row2, C2[1], "IXmlSearchStrategy", "interface", [
        "+ Name: string {get}"], [
        "+ Search(path: string, criteria: SearchCriteria): List<Conference>"], kind="interface")
    conf = p.class_box("conf", C3[0], row2, C3[1], "Conference", "entity", [
        "+ Attributes: Dictionary<string, string> {get}",
        "+ Dates: ConferenceDates?",
        "+ SubmissionDeadline: DateOnly?",
        "+ Title: string {get}"], [
        "+ GetAttribute(name: string): string?",
        "+ ToString(): string"], kind="entity")
    dates = p.class_box("dates", C4[0], max(row2, bottom(fmt) + 40), C4[1], "ConferenceDates", "record, entity", [
        '$+ DateFormat: string = "yyyy-MM-dd" {const}',
        "+ Start: DateOnly {get}",
        "+ End: DateOnly {get}"], [
        "+ ConferenceDates(Start: DateOnly, End: DateOnly)",
        "$+ Parse(start: string?, end: string?): ConferenceDates",
        "$+ ParseDate(text: string?, what: string): DateOnly",
        "$+ Format(d: DateOnly): string",
        "+ ToString(): string"], kind="entity")
    crit = p.class_box("crit", C3[0], bottom(conf) + 80, C3[1], "SearchCriteria", "entity", [
        "+ Keyword: string? {get}",
        "+ Filters: IReadOnlyDictionary<string, string> {get}"], [
        "+ SearchCriteria(keyword: string?,",
        "      filters: IReadOnlyDictionary<string, string>?)",
        "+ Matches(c: Conference): bool",
        "+ ToString(): string"], kind="entity")

    sy = bottom(itf) + 90
    sw, sg = 270, 30
    sx = [40, 40 + sw + sg, 40 + 2 * (sw + sg)]   # закінчується на 900 < 920
    p.class_box("sax", sx[0], sy, sw, "SaxSearchStrategy", None, [
        '+ Name: string = "SAX" {get}'], [
        "+ Search(path: string, criteria:",
        "      SearchCriteria): List<Conference>",
        "$- ReadConference(reader: XmlReader):",
        "$      Conference"], kind="strategy")
    p.class_box("dom", sx[1], sy, sw, "DomSearchStrategy", None, [
        '+ Name: string = "DOM" {get}'], [
        "+ Search(path: string, criteria:",
        "      SearchCriteria): List<Conference>",
        "$+ BuildXPath(criteria: SearchCriteria): string",
        "$+ Literal(value: string): string",
        "$- ToConference(element: XmlElement):",
        "$      Conference"], kind="strategy")
    p.class_box("lin", sx[2], sy, sw, "LinqSearchStrategy", None, [
        '+ Name: string = "LINQ to XML" {get}'], [
        "+ Search(path: string, criteria:",
        "      SearchCriteria): List<Conference>",
        "$- ToConference(e: XElement): Conference"], kind="strategy")
    B = p.boxes
    strat_bottom = max(bottom(B[k]) for k in ("sax", "dom", "lin"))

    n1y = max(strat_bottom, bottom(crit)) + 40
    p.note(40, n1y, 860, 92, "Шаблон «Стратегія»: той самий пошук трьома способами.\n"
           "SaxSearchStrategy: послідовне читання через XmlReader (SAX-підхід у .NET), у пам'яті лише одна конференція.\n"
           "DomSearchStrategy: дерево XmlDocument і динамічно згенерований запит XPath (BuildXPath).\n"
           "LinqSearchStrategy: XDocument і динамічний ланцюжок Where — по одному на кожен фільтр.")
    p.note(C4[0], bottom(dates) + 40, C4[1], 108,
           "XmlNames і XmlFormat використовують усі три\n"
           "стратегії, AttributeCatalog і ConferenceXmlWriter\n"
           "(зв'язки не показано, щоб не перевантажувати схему).\n"
           "XmlFormat забороняє DTD (захист від XXE).\n"
           "Підкреслені члени — статичні.")
    p.note(C3[0], n1y, C3[1], 76,
           "MauiProgram реєструє в контейнері залежностей усі\n"
           "стратегії, XmlSearchContext, AttributeCatalog,\n"
           "ConferenceXmlWriter, HtmlTransformer, MainViewModel\n"
           "і MainPage; Shell отримує MainPage з контейнера.")
    core_bottom = max(n1y + 108, bottom(dates) + 40 + 108) + 30
    p.package(20, CORE_TOP, p.w - 40, core_bottom - CORE_TOP, "XmlAnalyzer  (бібліотека логіки, без залежності від MAUI)")
    # пакети малюються під класами: переносимо їх на початок
    for lst in (p.svg, p.cells):
        pass
    p.h = core_bottom + 20

    # ---------------- зв'язки застосунку ----------------
    mpb, appb, shb = B["mp"], B["app"], B["shell"]
    cx1 = C1[0] + C1[1] / 2
    p.edge([(cx1, bottom(mpb)), (cx1, appb["y"])], "dep", "«create»", (cx1, (bottom(mpb) + appb["y"]) / 2), src="mp", dst="app")
    p.edge([(cx1, bottom(appb)), (cx1, shb["y"])], "dep", "«create»", (cx1, (bottom(appb) + shb["y"]) / 2), src="app", dst="shell")
    sy_ = mid(shb)[1]
    p.edge([(right(shb), sy_), (page["x"], sy_)], "dep", "«create»", ((right(shb) + page["x"]) / 2, sy_ - 11), src="shell", dst="page")
    # MainPage -> FileDialogs, ResultRow
    p.edge([(right(page), fd["y"] + 40), (fd["x"], fd["y"] + 40)], "dep", "«use»", ((right(page) + fd["x"]) / 2, fd["y"] + 29), src="page", dst="fd")
    p.edge([(right(page), rr["y"] + 40), (rr["x"], rr["y"] + 40)], "dep", "«create»", ((right(page) + rr["x"]) / 2, rr["y"] + 29), src="page", dst="rr")
    p.edge([(right(rr), al["y"] + 36), (al["x"], al["y"] + 36)], "dep", "«use»", ((right(rr) + al["x"]) / 2, al["y"] + 25), src="rr", dst="al")
    # MainPage -> MainViewModel
    vx = vm["x"] + 120
    p.edge([(vx, bottom(page)), (vx, vm["y"])], "assoc", "1", (vx + 12, vm["y"] - 12), src="page", dst="vm")
    # ResultRow ..> Conference (через прохід між C3 і C4)
    gx = (right(rr) + C4[0]) / 2
    p.edge([(right(rr), bottom(rr) - 20), (gx, bottom(rr) - 20), (gx, conf["y"] + 22), (right(conf), conf["y"] + 22)],
           "dep", "«use»", (gx, (bottom(rr) + conf["y"]) / 2), src="rr", dst="conf")

    # ---------------- зв'язки ядра ----------------
    cy = ctx["y"] + 40
    p.edge([(vm["x"], cy), (right(ctx), cy)], "assoc", "1", (right(ctx) + 12, cy - 11), src="vm", dst="ctx")
    for key, b in (("cat", cat), ("tr", tr), ("wr", wr)):
        yy = b["y"] + 36
        p.edge([(right(vm), yy), (b["x"], yy)], "assoc", "1", (b["x"] - 12, yy - 11), src="vm", dst=key)
    ix = itf["x"] + itf["w"] / 2
    p.edge([(ix, bottom(vm)), (ix, itf["y"])], "agg", "3  Strategies", (ix + 52, itf["y"] - 22), src="vm", dst="itf")
    ccx = ctx["x"] + 150
    iy = itf["y"] + 44
    p.edge([(ccx, bottom(ctx)), (ccx, iy), (itf["x"], iy)], "agg", "1", (itf["x"] - 12, iy - 11), src="ctx", dst="itf")
    # канали між C2 (до 920) і C3 (від 1040)
    k1, k2, k3 = 945, 965, 985
    p.edge([(right(vm), bottom(vm) - 40), (k3, bottom(vm) - 40), (k3, conf["y"] + 64), (conf["x"], conf["y"] + 64)],
           "assoc", "0..*", (conf["x"] - 16, conf["y"] + 53), src="vm", dst="conf")
    p.edge([(right(vm), bottom(vm) - 18), (k2, bottom(vm) - 18), (k2, crit["y"] + 64), (crit["x"], crit["y"] + 64)],
           "dep", "«create»", (k2, crit["y"] - 20), src="vm", dst="crit")
    p.edge([(right(itf), itf["y"] + 26), (conf["x"], itf["y"] + 26)], "dep", "«create»", (1013, itf["y"] + 14), src="itf", dst="conf")
    p.edge([(right(itf), bottom(itf) - 14), (k1, bottom(itf) - 14), (k1, crit["y"] + 34), (crit["x"], crit["y"] + 34)],
           "dep", "«use»", (k1, bottom(itf) + 30), src="itf", dst="crit")
    wx = C3[0] + C3[1] / 2
    p.edge([(wx, bottom(wr)), (wx, conf["y"])], "dep", "«use»", (wx, (bottom(wr) + conf["y"]) / 2), src="wr", dst="conf")
    p.edge([(wx, crit["y"]), (wx, bottom(conf))], "dep", "«use»", (wx, (crit["y"] + bottom(conf)) / 2), src="crit", dst="conf")
    dy_ = dates["y"] + 50
    if dy_ > bottom(conf) - 10:
        warnings.append("composition y outside Conference")
    p.edge([(right(conf), dy_), (dates["x"], dy_)], "comp", "0..1", (dates["x"] - 18, dy_ - 11), src="conf", dst="dates")
    busy = sy - 45
    for key, x in zip(("sax", "dom", "lin"), sx):
        cx = x + sw / 2
        pts = [(cx, sy), (cx, busy), (ix, busy), (ix, bottom(itf))] if abs(cx - ix) > .5 else [(cx, sy), (ix, bottom(itf))]
        p.edge(pts, "real", src=key, dst="itf")
    return p


# =====================================================================
# 3. SEQUENCE
# =====================================================================
def sequence():
    names = [("user", 75, "Користувач"), ("page", 300, ":MainPage"), ("vm", 510, ":MainViewModel"),
             ("ctx", 720, ":XmlSearchContext"), ("str", 930, ":IXmlSearchStrategy"), ("crit", 1130, ":SearchCriteria")]
    X = {k: x for k, x, _ in names}
    ST = {"page": "boundary", "vm": "control", "ctx": "control", "str": "control", "crit": "entity"}
    p = Page("sq", "3 - Sequence", 1250, 100)
    HEAD_Y, HEAD_H = 66, 46

    def head(x, hy, w, nm, k, height):
        st = ST[k]
        p.svg.append(f'<rect x="{x-w/2}" y="{hy}" width="{w}" height="{HEAD_H}" fill="{FILLS[st]}" stroke="{INK}" stroke-width="1.2"/>')
        p.svg_text(x, hy + 17, f"«{st}»", 11, italic=True)
        p.svg_text(x, hy + 35, nm, 13)
        p.vertex(x - w / 2, hy, w, height, f"«{st}»<br>{nm}",
                 f"shape=umlLifeline;perimeter=lifelinePerimeter;html=1;container=0;collapsible=0;recursiveResize=0;size={HEAD_H};fontSize=13;fillColor={FILLS[st]};strokeColor={INK};")

    msgs, frames, bars = [], [], []
    y = [HEAD_Y + HEAD_H + 34]
    num = [0]

    def m(a, b, label, kind="sync", step=34):
        # нумеруються лише виклики; повернення підписуються значенням
        if kind != "ret":
            num[0] += 1
            label = f"{num[0]}: {label}"
        msgs.append(dict(a=a, b=b, label=label, kind=kind, y=y[0]))
        y[0] += step
        return msgs[-1]["y"]

    def self_(a, label):
        num[0] += 1
        msgs.append(dict(a=a, b=a, label=f"{num[0]}: {label}", kind="self", y=y[0]))
        y[0] += 50
        return msgs[-1]["y"]

    def gap(h):
        y[0] += h

    s1 = m("user", "page", "CheckedChanged(sender, e)")
    m("user", "page", "OnKeywordChanged(sender, e)")
    m("user", "page", "SelectedIndexChanged(sender, e)")
    m("user", "page", "OnSearchClicked(sender, e)")
    oa = y[0] - 12
    gap(30)
    self_("page", 'DisplayAlertAsync(title, message, "OK")')
    ob = y[0] - 8
    frames.append((40, oa, 580, ob - oa, "opt", "[XmlPath == null]", [], None))
    gap(26)
    s4 = m("page", "vm", "Search()")
    critY = y[0]
    m("vm", "crit", "«create» SearchCriteria(Keyword, SelectedFilters)", "create")
    s6 = m("vm", "ctx", "SetStrategy(SelectedStrategy)")
    r6 = s6 + 10
    s7 = m("vm", "ctx", "Execute(XmlPath, criteria)")
    s8 = m("ctx", "str", "Search(path, criteria)")
    la = y[0] - 12
    gap(30)
    s10 = m("str", "crit", "Matches(conference)")
    r11 = m("crit", "str", "bool", "ret", step=30)
    lb = y[0] - 8
    bars.append(("crit", s10, r11))
    frames.append((845, la, 370, lb - la, "loop", "[foreach Conference]", [], 942))
    gap(20)
    r12 = m("str", "ctx", "List<Conference>", "ret")
    bars.append(("str", s8, r12))
    r13 = m("ctx", "vm", "List<Conference>", "ret")
    bars.append(("ctx", s7, r13))
    bars.append(("ctx", s6, r6))
    r14 = m("vm", "page", "void", "ret")
    bars.append(("vm", s4, r14))
    self_("page", "ShowResults()")
    bars.append(("page", s1, y[0] - 20))
    end = y[0] + 14

    fx, fy, fw, fh = 14, 12, p.w - 28, end + 14 - 12
    p.h = fy + fh + 14

    def frame(x, yy, w, h, kw, guard=None, dividers=(), gx=None):
        tabw = max(tw(kw, 12) + 22, 42)
        p.svg.append(f'<rect x="{x}" y="{yy}" width="{w}" height="{h}" fill="none" stroke="{INK}" stroke-width="1.1"/>'
                     f'<path d="M{x} {yy+20}H{x+tabw-8}L{x+tabw} {yy+12}V{yy}H{x}Z" fill="#ffffff" stroke="{INK}" stroke-width="1.1"/>')
        p.svg_text(x + 6, yy + 15, kw, 12, "start", bold=True)
        p.vertex(x, yy, w, h, kw, f"shape=umlFrame;whiteSpace=nowrap;html=1;width={round(tabw)};height=20;fontSize=12;fontStyle=1;fillColor=none;strokeColor={INK};boundedLbl=0;")
        if guard:
            p.text(gx or (x + tabw + 6), yy + 11, guard, 12, "start")
        for dy, g in dividers:
            p.edge([(x, dy), (x + w, dy)], "dash")
            p.text(x + 8, dy + 11, g, 12, "start")

    frame(fx, fy, fw, fh, "sd Пошук конференцій")
    for f in frames:
        frame(*f)

    bottom_ = end
    for k, x, nm in names:
        top = HEAD_Y + HEAD_H
        if k == "crit":
            hy = critY - 23
            top = hy + HEAD_H
            p.svg.append(f'<path d="M{x} {top}V{bottom_}" stroke="{INK}" stroke-width="1" stroke-dasharray="5 4"/>')
            head(x, hy, 136, nm, k, bottom_ - hy)
            continue
        if k == "user":
            p.svg.append(f'<path d="M{x} {top + 14}V{bottom_}" stroke="{INK}" stroke-width="1" stroke-dasharray="5 4"/>')
            p.actor_shape(x, 36, 52)
            p.vertex(x - 14, 36, 28, 52, "Користувач",
                     f"shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;fontSize=13;fillColor=#ffffff;strokeColor={INK};")
            p.cells.append(f'<mxCell id="{p.nid()}" value="" style="html=1;edgeStyle=none;dashed=1;endArrow=none;strokeColor={INK};" edge="1" parent="1">'
                           f'<mxGeometry relative="1" as="geometry"><mxPoint x="{x}" y="{top + 14}" as="sourcePoint"/><mxPoint x="{x}" y="{bottom_}" as="targetPoint"/></mxGeometry></mxCell>')
            continue
        p.svg.append(f'<path d="M{x} {top}V{bottom_}" stroke="{INK}" stroke-width="1" stroke-dasharray="5 4"/>')
        w = max(tw(nm, 13) + 18, 130)
        head(x, HEAD_Y, w, nm, k, bottom_ - HEAD_Y)

    BW = 12
    for k, a, b_ in bars:
        x = X[k]
        p.svg.append(f'<rect x="{x-BW/2}" y="{a}" width="{BW}" height="{b_-a}" fill="#eaf2fb" stroke="{INK}" stroke-width="1"/>')
        p.vertex(x - BW / 2, a, BW, b_ - a, "", f"html=1;fillColor=#eaf2fb;strokeColor={INK};")

    for mg in msgs:
        a, b_, yy = X[mg["a"]], X[mg["b"]], mg["y"]
        k = mg["kind"]
        if k == "self":
            x0 = a + BW / 2
            p.edge([(x0, yy), (x0 + 34, yy), (x0 + 34, yy + 24), (x0, yy + 24)], "sync")
            p.text(x0 + 40, yy + 12, mg["label"], 12, "start", halo=True)
            continue
        d = 1 if b_ > a else -1
        x0 = a + d * BW / 2 if mg["a"] != "user" else a
        x1 = b_ - d * BW / 2 if mg["b"] != "user" else b_
        if k == "create":
            x1 = b_ - 68
        p.edge([(x0, yy), (x1, yy)], "ret" if k in ("ret", "create") else "sync")
        lab = mg["label"]
        if tw(lab, 12) > abs(x1 - x0) - 6:
            p.text(min(x0, x1) + 6, yy - 9, lab, 12, "start", halo=True)
        else:
            p.text((x0 + x1) / 2, yy - 9, lab, 12, "middle", halo=True)
    return p


pages = [use_case(), class_diagram(), sequence()]

# Пакети на діаграмі класів мають бути позаду класів: переносимо їх на початок списків.
cl = pages[1]
for lst, marker in ((cl.svg, 'stroke="#9a8a92"'), (cl.cells, "shape=folder")):
    pk = [e for e in lst if marker in e]
    for e in pk:
        lst.remove(e)
    lst[:0] = pk
# підпис пакета в SVG — окремий текст після path; переносимо і його
pk_text = [e for e in cl.svg if 'fill="#5a4650"' in e]
for e in pk_text:
    cl.svg.remove(e)
cl.svg[2:2] = pk_text

mx = ('<?xml version="1.0" encoding="UTF-8"?>\n<mxfile host="app.diagrams.net" type="device">'
      + "".join(pg.to_diagram() for pg in pages) + "</mxfile>\n")
open(os.path.join(OUT, "lab2_uml.drawio"), "w", encoding="utf-8").write(mx)
for pg, fn in zip(pages, ["1_use_case", "2_class", "3_sequence"]):
    open(os.path.join(OUT, fn + ".svg"), "w", encoding="utf-8").write(pg.to_svg())
print("WARN:", *warnings, sep="\n  ") if warnings else print("no warnings")
for pg in pages:
    print(pg.name, pg.w, pg.h)
