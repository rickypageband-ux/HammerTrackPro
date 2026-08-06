from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import inch
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfbase import pdfmetrics
from reportlab.platypus import Paragraph, Table, TableStyle
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output" / "pdf" / "HammerTrackPro-Two-Computer-Installation-Guide.pdf"
LOGO = ROOT / "HammerTrackPro2" / "Assets" / "HammerTrackPro.png"

NAVY = colors.HexColor("#0A3572")
BLUE = colors.HexColor("#155AA5")
GOLD = colors.HexColor("#F0B323")
PALE_GOLD = colors.HexColor("#FFF6D9")
PALE_BLUE = colors.HexColor("#EAF3FB")
LINE = colors.HexColor("#CCD8E5")
INK = colors.HexColor("#22313F")
MUTED = colors.HexColor("#5C6B78")

W, H = letter
LEFT, RIGHT = 0.65 * inch, W - 0.65 * inch
CONTENT_W = RIGHT - LEFT

BODY = ParagraphStyle("body", fontName="Helvetica", fontSize=8.3, leading=11, textColor=INK)
SMALL = ParagraphStyle("small", parent=BODY, fontSize=7.4, leading=9.3)
TINY = ParagraphStyle("tiny", parent=BODY, fontSize=6.8, leading=8.2)
HEAD = ParagraphStyle("head", fontName="Helvetica-Bold", fontSize=18, leading=21, textColor=NAVY)
SECTION = ParagraphStyle("section", fontName="Helvetica-Bold", fontSize=11, leading=13, textColor=BLUE)
WHITE_BOLD = ParagraphStyle("white", fontName="Helvetica-Bold", fontSize=8, leading=10, textColor=colors.white)


def p(text, style=BODY):
    return Paragraph(text, style)


def header(c, title, page):
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 7.3)
    c.drawString(LEFT, H - 0.42 * inch, "HAMMERTRACK PRO")
    c.setFillColor(MUTED)
    c.setFont("Helvetica", 6.8)
    c.drawRightString(RIGHT, H - 0.42 * inch, "Two-Computer Installation Guide")
    c.setStrokeColor(LINE)
    c.line(LEFT, H - 0.53 * inch, RIGHT, H - 0.53 * inch)
    c.setFillColor(NAVY)
    c.setFont("Helvetica-Bold", 18)
    c.drawString(LEFT, H - 0.92 * inch, title)
    footer(c, page)


def footer(c, page):
    c.setStrokeColor(LINE)
    c.line(LEFT, 0.48 * inch, RIGHT, 0.48 * inch)
    c.setFillColor(MUTED)
    c.setFont("Helvetica", 6.5)
    c.drawString(LEFT, 0.31 * inch, "Keep this guide with the live-auction equipment.")
    c.drawRightString(RIGHT, 0.31 * inch, f"Page {page}")


def paragraph(c, text, x, y, width=CONTENT_W, style=BODY):
    item = p(text, style)
    _, height = item.wrap(width, H)
    item.drawOn(c, x, y - height)
    return y - height


def section(c, text, y):
    return paragraph(c, text, LEFT, y, CONTENT_W, SECTION) - 4


def callout(c, text, y, color=PALE_GOLD, border=GOLD):
    item = p(f"<b>{text}</b>", SMALL)
    _, height = item.wrap(CONTENT_W - 18, H)
    box_h = height + 14
    c.setFillColor(color)
    c.setStrokeColor(border)
    c.rect(LEFT, y - box_h, CONTENT_W, box_h, fill=1, stroke=1)
    item.drawOn(c, LEFT + 9, y - height - 7)
    return y - box_h - 10


def table(c, rows, y, widths, style=SMALL, header_rows=0):
    data = [[p(str(cell), WHITE_BOLD if header_rows and r < header_rows else style) for cell in row] for r, row in enumerate(rows)]
    t = Table(data, colWidths=widths, hAlign="LEFT")
    commands = [
        ("GRID", (0, 0), (-1, -1), 0.55, LINE),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 7),
        ("RIGHTPADDING", (0, 0), (-1, -1), 7),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("BACKGROUND", (0, 0), (0, -1), PALE_BLUE),
    ]
    if header_rows:
        commands.extend([("BACKGROUND", (0, 0), (-1, header_rows - 1), NAVY)])
    t.setStyle(TableStyle(commands))
    _, height = t.wrap(CONTENT_W, H)
    t.drawOn(c, LEFT, y - height)
    return y - height - 10


def step(c, number, title, text, y):
    box = 22
    y2 = paragraph(c, f"<b>{number}. {title}</b><br/>{text}", LEFT + box + 10, y + 1, CONTENT_W - box - 10, SMALL)
    c.setFillColor(BLUE)
    c.rect(LEFT, y - box + 2, box, box, fill=1, stroke=0)
    return min(y - box, y2) - 8


def checklist(c, items, y):
    for item in items:
        y = paragraph(c, f"[ ] {item}", LEFT + 4, y, CONTENT_W - 4, SMALL) - 4
    return y


def page_one(c):
    c.setTitle("HammerTrack Pro Two-Computer Installation Guide")
    c.drawImage(str(LOGO), W / 2 - 0.62 * inch, H - 2.45 * inch, 1.24 * inch, 1.24 * inch, preserveAspectRatio=True, mask="auto")
    title = Paragraph("Two-Computer<br/>Installation Guide", ParagraphStyle("cover", parent=HEAD, alignment=TA_CENTER, fontSize=23, leading=25))
    _, h = title.wrap(4.5 * inch, H)
    title.drawOn(c, W / 2 - 2.25 * inch, H - 3.17 * inch - h)
    c.setFillColor(MUTED); c.setFont("Helvetica", 10); c.drawCentredString(W / 2, H - 4.20 * inch, "Live Auction Host + Cashier Workstation")
    y = H - 4.55 * inch
    y = callout(c, "Use this sequence exactly: install both PCs, collect both Machine IDs, activate both PCs, configure the host, then pair the cashier.", y)
    y = table(c, [
        ["Installer", "HammerTrackPro-0.7.35-x64-signed.msi"],
        ["Supported setup", "Two Windows x64 computers on the same private local network"],
        ["Computer 1", "Live Auction Host - owns the auction database"],
        ["Computer 2", "Cashier Workstation - connects securely to the host"],
    ], y, [1.25 * inch, CONTENT_W - 1.25 * inch])
    y = section(c, "Important", y)
    y = paragraph(c, "Never place or share the HammerTrack database on a network drive. The host keeps the database locally; the cashier communicates with the host through HammerTrack's paired HTTPS connection.", LEFT, y)
    paragraph(c, "Print this guide in color or grayscale at 100% scale on US Letter paper.", LEFT, y - 10, CONTENT_W, TINY)
    footer(c, 1)


def page_two(c):
    header(c, "1. Before you begin", 2)
    y = H - 1.17 * inch
    y = paragraph(c, "Gather everything before auction day. Perform the installation while both computers can remain on the same private network.", LEFT, y) - 10
    y = section(c, "You will need", y)
    y = checklist(c, [
        "The HammerTrack Pro 0.7.35 signed MSI installer on a USB drive or other trusted transfer method.",
        "Administrator permission on both Windows computers.",
        "One host computer for live-auction operation and one cashier computer.",
        "Both computers connected to the same trusted private router or LAN.",
        "A signed .htplicense file that includes the Machine ID of both computers.",
    ], y) - 4
    y = section(c, "Choose and label the computers", y)
    y = table(c, [
        ["LIVE AUCTION HOST", "Runs the auction, stores the local database, and must stay powered on while the cashier is working."],
        ["CASHIER", "Looks up current-auction buyers and sellers and records payments through the secure connection to the host."],
    ], y, [1.85 * inch, CONTENT_W - 1.85 * inch])
    y = callout(c, "Do not use public Wi-Fi. If possible, connect both PCs by Ethernet to the same router for the most reliable auction-day connection.", y)
    y = section(c, "Record for this installation", y)
    table(c, [
        ["Auction company", "________________________________________________________"],
        ["Host PC name", "________________________________________________________"],
        ["Cashier PC name", "________________________________________________________"],
        ["Installation date", "________________________________________________________"],
    ], y, [1.75 * inch, CONTENT_W - 1.75 * inch])


def page_three(c):
    header(c, "2. Install and activate both computers", 3)
    y = H - 1.16 * inch
    y = step(c, 1, "Install on the host", "Copy HammerTrackPro-0.7.35-x64-signed.msi to the live-auction computer. Double-click it, approve the Windows administrator prompt, and complete the installer.", y)
    y = step(c, 2, "Install on the cashier", "Repeat the same signed MSI installation on the cashier computer.", y)
    y = step(c, 3, "Launch HammerTrack Pro", "Use the Desktop or Start Menu shortcut. The installer is locally self-signed. If Windows displays an unknown-publisher warning on a computer that does not trust the local certificate, confirm the MSI came from your trusted HammerTrack provider before continuing.", y)
    y = step(c, 4, "Collect both Machine IDs", "At the activation screen, select Copy Machine ID on each computer. Label each ID as Host or Cashier and send both IDs to the license issuer.", y)
    y = step(c, 5, "Receive one license file", "The issuer creates one signed .htplicense file containing both Machine IDs. Keep the file private and unchanged.", y)
    y = step(c, 6, "Activate both computers", "On each activation screen, import the same .htplicense file. HammerTrack confirms the licensed customer name.", y)
    y = callout(c, "A license for only one Machine ID will not activate the other PC. If either computer is replaced, request a replacement license containing the complete approved Machine ID list.", y)
    y = section(c, "Activation record", y)
    table(c, [
        ["Host Machine ID", "_______________________________________________"],
        ["Cashier Machine ID", "_______________________________________________"],
        ["License file received", "________________________  Date: ______________"],
    ], y, [1.8 * inch, CONTENT_W - 1.8 * inch])


def page_four(c):
    header(c, "3. Configure the Live Auction Host", 4)
    y = H - 1.16 * inch
    y = step(c, 1, "Select the host role", "On first setup, choose Live Auction Host (stores the database), then save the selection. Restart HammerTrack if prompted or if you later change the role.", y)
    y = step(c, 2, "Allow private-network access", "If Windows Firewall asks, allow HammerTrack on Private networks. Do not enable it for public networks.", y)
    y = step(c, 3, "Open Network Information", "In HammerTrack, open Network Information. Leave this screen available while setting up the cashier.", y)
    y = step(c, 4, "Copy the pairing values", "Record the Host address, Alternate IP address, Pairing key, and Certificate fingerprint exactly as displayed. Keep the pairing key private.", y)
    y = step(c, 5, "Keep the host running", "HammerTrack must remain open on this computer whenever the cashier workstation is being used.", y)
    y = section(c, "Host pairing record", y)
    y = table(c, [
        ["Host address", "_______________________________________________"],
        ["Alternate IP address", "_______________________________________________"],
        ["Pairing key", "_______________________________________________\n_______________________________________________"],
        ["Certificate fingerprint", "_______________________________________________\n_______________________________________________"],
    ], y, [1.8 * inch, CONTENT_W - 1.8 * inch])
    callout(c, "The live-auction host is the only computer that opens the SQLite database. Never copy the live database to a shared, UNC, mapped-network, or cloud-synced folder.", y, PALE_BLUE, BLUE)


def page_five(c):
    header(c, "4. Pair the Cashier Workstation", 5)
    y = H - 1.16 * inch
    y = step(c, 1, "Confirm the host is ready", "The host PC must be running HammerTrack and both computers must be on the same private network.", y)
    y = step(c, 2, "Select the cashier role", "On the cashier setup screen, choose Cashier Workstation (connects to host).", y)
    y = step(c, 3, "Enter the secure connection values", "Copy the Host address, Pairing key, and Certificate fingerprint from Network Information on the host. Include the full HTTPS address.", y)
    y = step(c, 4, "Save and restart", "Save the workstation setup. Restart HammerTrack after changing the workstation role or connection settings.", y)
    y = step(c, 5, "Verify connection status", "Open the cashier screen and confirm it connects to the host. The cashier retries health checks after a temporary disconnect.", y)
    y = callout(c, "If the computer-name Host address does not connect, edit cashier setup and use the Alternate IP address shown on the host. Keep the same pairing key and certificate fingerprint.", y)
    y = section(c, "Run a pre-auction cashier test", y)
    y = checklist(c, [
        "On the host, select the correct Current Auction.",
        "On the cashier, confirm the connected state and current-auction context.",
        "Look up a test Buyer registered for that auction and confirm the expected purchases and totals.",
        "Open Add Buyer and Add Seller; confirm existing Buyer Numbers appear for Seller linking.",
        "Verify an existing Buyer can be registered as a Seller using the same number without creating a duplicate Buyer.",
        "Use Full Screen or F11 and confirm Escape exits full screen.",
        "If using license scanning, test a license with the barcode level, close, sharp, and filling most of the dashed guide.",
        "For a handheld scanner, enable PDF417 and USB HID/keyboard mode. In Use Handheld Scanner, verify the received-character count increases and the bidder fields fill after the scan.",
        "Do not record a real payment during the connection test unless it is intended and auditable.",
    ], y)
    y = section(c, "Security note", y - 2)
    paragraph(c, "The cashier pins the host certificate fingerprint and uses the pairing key. Do not bypass certificate or pairing errors; compare the values against the host's Network Information screen.", LEFT, y, CONTENT_W, TINY)


def page_six(c):
    header(c, "5. Troubleshooting and auction-day checklist", 6)
    y = H - 1.35 * inch
    y = section(c, "If the cashier will not connect", y)
    y = table(c, [
        ["Disconnected", "Confirm HammerTrack is open on the host and both PCs are connected to the same private router/LAN."],
        ["Host name fails", "Use the Alternate IP address from the host's Network Information screen."],
        ["Not paired", "Re-enter the pairing key exactly. Do not add spaces before or after it."],
        ["Fingerprint error", "Re-enter the certificate fingerprint exactly as displayed on the host. Do not ignore a mismatch."],
        ["Still offline", "Confirm Windows classified the network as Private and HammerTrack is allowed through the host firewall on Private networks."],
    ], y, [1.35 * inch, CONTENT_W - 1.35 * inch], TINY)
    y = section(c, "If activation fails", y)
    y = table(c, [
        ["PC not included", "Send the displayed Machine ID to the license issuer and request a replacement license that retains all approved PCs."],
        ["Invalid file", "Import the original .htplicense file without renaming its extension or editing its contents."],
        ["Expired license", "Contact the license issuer for a renewed signed license."],
    ], y, [1.35 * inch, CONTENT_W - 1.35 * inch], TINY)
    y = section(c, "Before bidders arrive", y)
    y = checklist(c, [
        "Host PC is connected to power and sleep is disabled for the event.",
        "Cashier PC is connected and shows a healthy connection.",
        "Correct Current Auction is selected on the host.",
        "A recent in-app backup exists in the local Backups folder.",
        "Network Information is available to the operator but the pairing key is not publicly visible.",
        "Buyer lookup and same-number Seller linking succeed from the cashier.",
    ], y)
    y = callout(c, "Data location: %ProgramData%\\HammerTrackPro. Application upgrades and uninstall preserve the database, backups, logs, settings, and license, but maintain current backups before any upgrade or service work.", y, PALE_BLUE, BLUE)
    paragraph(c, "Installation completed by: ____________________________________   Date: __________________", LEFT, y, CONTENT_W, SMALL)


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUTPUT), pagesize=letter, pageCompression=1)
    for index, page_fn in enumerate((page_one, page_two, page_three, page_four, page_five, page_six)):
        page_fn(c)
        if index < 5:
            c.showPage()
    c.save()
    print(OUTPUT)


if __name__ == "__main__":
    build()
