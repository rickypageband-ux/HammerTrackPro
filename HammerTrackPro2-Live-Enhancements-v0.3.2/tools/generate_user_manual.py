from pathlib import Path
from PIL import Image as PILImage, ImageDraw, ImageFont
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.platypus import (BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, PageBreak,
                                Image, Table, TableStyle, KeepTogether, NextPageTemplate)
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfbase import pdfmetrics
from reportlab.lib.utils import ImageReader

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'output'/'pdf'/'HammerTrackPro-User-Manual.pdf'
ASSETS=ROOT/'output'/'manual-assets'
ANNOTATED=ROOT/'tmp'/'manual-annotated'
LOGO=ROOT/'HammerTrackPro2'/'Assets'/'HammerTrackPro.png'
VERSION='0.7.50'
NAVY=colors.HexColor('#18324A'); BLUE=colors.HexColor('#186096'); GOLD=colors.HexColor('#E0A836')
PALE=colors.HexColor('#F2F5F8'); LIGHTBLUE=colors.HexColor('#E7F1F8'); WARN=colors.HexColor('#FFF4D6')
RED=colors.HexColor('#A52A2A'); GREEN=colors.HexColor('#1F6B3B'); GRAY=colors.HexColor('#5C6670')

for name,path in [('SegoeUI','C:/Windows/Fonts/segoeui.ttf'),('SegoeUI-Bold','C:/Windows/Fonts/seguisb.ttf')]:
    if Path(path).exists(): pdfmetrics.registerFont(TTFont(name,path))
FONT='SegoeUI' if 'SegoeUI' in pdfmetrics.getRegisteredFontNames() else 'Helvetica'
BOLD='SegoeUI-Bold' if 'SegoeUI-Bold' in pdfmetrics.getRegisteredFontNames() else 'Helvetica-Bold'

styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='ManualBody',fontName=FONT,fontSize=9.7,leading=13.2,textColor=NAVY,spaceAfter=7))
styles.add(ParagraphStyle(name='ManualH1',fontName=BOLD,fontSize=21,leading=24,textColor=NAVY,spaceBefore=3,spaceAfter=12,keepWithNext=True))
styles.add(ParagraphStyle(name='ManualH2',fontName=BOLD,fontSize=14,leading=17,textColor=BLUE,spaceBefore=10,spaceAfter=7,keepWithNext=True))
styles.add(ParagraphStyle(name='ManualH3',fontName=BOLD,fontSize=11.5,leading=14,textColor=NAVY,spaceBefore=7,spaceAfter=4,keepWithNext=True))
styles.add(ParagraphStyle(name='Step',fontName=FONT,fontSize=9.6,leading=13,textColor=NAVY,leftIndent=18,firstLineIndent=-18,spaceAfter=5))
styles.add(ParagraphStyle(name='Caption',fontName=FONT,fontSize=8.2,leading=10,textColor=GRAY,alignment=TA_CENTER,spaceBefore=4,spaceAfter=9))
styles.add(ParagraphStyle(name='TOCHeading',fontName=BOLD,fontSize=22,leading=25,textColor=NAVY,spaceAfter=14))
styles.add(ParagraphStyle(name='TOC1',fontName=BOLD,fontSize=10.5,leading=14,leftIndent=0,firstLineIndent=0,textColor=NAVY,spaceBefore=4))
styles.add(ParagraphStyle(name='TOC2',fontName=FONT,fontSize=9.2,leading=12,leftIndent=16,firstLineIndent=0,textColor=BLUE))

class ManualDocTemplate(BaseDocTemplate):
    def __init__(self,filename):
        super().__init__(filename,pagesize=letter,rightMargin=.68*inch,leftMargin=.68*inch,topMargin=.66*inch,bottomMargin=.62*inch,title='HammerTrack Pro User Manual',author='HammerTrack Pro')
        frame=Frame(self.leftMargin,self.bottomMargin,self.width,self.height,id='normal')
        self.addPageTemplates([PageTemplate(id='body',frames=frame,onPage=self._page)])
    def _page(self,canvas,doc):
        if doc.page==1:return
        canvas.saveState();canvas.setStrokeColor(colors.HexColor('#D8E0E7'));canvas.line(.68*inch,letter[1]-.43*inch,letter[0]-.68*inch,letter[1]-.43*inch)
        canvas.setFillColor(GRAY);canvas.setFont(FONT,7.8);canvas.drawString(.68*inch,.34*inch,f'HammerTrack Pro User Manual | Version {VERSION}')
        canvas.drawRightString(letter[0]-.68*inch,.34*inch,f'Page {doc.page}')
        canvas.restoreState()
    def afterFlowable(self,flowable):
        if isinstance(flowable,Paragraph):
            style=flowable.style.name
            if style in ('ManualH1','ManualH2'):
                level=0 if style=='ManualH1' else 1
                text=flowable.getPlainText();key='h%d-%s'%(level,self.seq.nextf('heading'))
                self.canv.bookmarkPage(key);self.canv.addOutlineEntry(text,key,level,False)
                self.notify('TOCEntry',(level,text,self.page,key))

def p(text,style='ManualBody'):return Paragraph(text,styles[style])
def h1(text):return p(text,'ManualH1')
def h2(text):return p(text,'ManualH2')
def h3(text):return p(text,'ManualH3')
def steps(items):return [p(f'<b>Step {i}:</b> {text}','Step') for i,text in enumerate(items,1)]
def bullets(items):return [p(f'-&nbsp;&nbsp;{text}','Step') for text in items]

def callout(title,text,kind='tip'):
    fill={'tip':LIGHTBLUE,'warning':WARN,'important':colors.HexColor('#FCE7E7')}[kind]
    accent={'tip':BLUE,'warning':GOLD,'important':RED}[kind]
    table=Table([[Paragraph(f'<b>{title}</b><br/>{text}',styles['ManualBody'])]],colWidths=[7.02*inch])
    table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,-1),fill),('BOX',(0,0),(-1,-1),1,accent),('LEFTPADDING',(0,0),(-1,-1),10),('RIGHTPADDING',(0,0),(-1,-1),10),('TOPPADDING',(0,0),(-1,-1),8),('BOTTOMPADDING',(0,0),(-1,-1),7)]))
    return table

def data_table(rows,widths=None):
    widths=widths or [2.0*inch,5.02*inch]
    table=Table([[Paragraph(str(c),styles['ManualBody']) for c in row] for row in rows],colWidths=widths,repeatRows=1)
    table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),NAVY),('TEXTCOLOR',(0,0),(-1,0),colors.white),('FONTNAME',(0,0),(-1,0),BOLD),('GRID',(0,0),(-1,-1),.5,colors.HexColor('#B9C5D0')),('VALIGN',(0,0),(-1,-1),'TOP'),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,PALE]),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6)]))
    return table

def annotated(source,name,boxes):
    ANNOTATED.mkdir(parents=True,exist_ok=True);dest=ANNOTATED/name
    image=PILImage.open(ASSETS/source).convert('RGB');draw=ImageDraw.Draw(image);w,h=image.size
    font_path='C:/Windows/Fonts/seguisb.ttf';font=ImageFont.truetype(font_path,max(18,int(min(w,h)*.025))) if Path(font_path).exists() else ImageFont.load_default()
    for number,(x1,y1,x2,y2) in enumerate(boxes,1):
        rect=(int(x1*w),int(y1*h),int(x2*w),int(y2*h));line=max(4,int(min(w,h)*.006));draw.rounded_rectangle(rect,radius=line*2,outline=(214,35,35),width=line)
        r=max(16,int(min(w,h)*.026));cx=max(r+2,rect[0]);cy=max(r+2,rect[1]);draw.ellipse((cx-r,cy-r,cx+r,cy+r),fill=(214,35,35),outline='white',width=max(2,line//2));label=str(number);bbox=draw.textbbox((0,0),label,font=font);draw.text((cx-(bbox[2]-bbox[0])/2,cy-(bbox[3]-bbox[1])/2-2),label,font=font,fill='white')
    image.save(dest,quality=94);return dest

def figure(path,caption,width=7.02*inch):
    with PILImage.open(path) as im:ratio=im.height/im.width
    return KeepTogether([Image(str(path),width=width,height=width*ratio),p(caption,'Caption')])

dashboard=annotated('dashboard.png','dashboard-annotated.jpg',[(.59,.40,.97,.81),(.12,.42,.57,.77),(.78,.03,.99,.10)])
setup=annotated('auction-setup.png','setup-annotated.jpg',[(.01,.12,.31,.92),(.32,.12,.99,.91),(.84,.91,.99,.99)])
seller=annotated('seller-management.png','seller-annotated.jpg',[(.01,.10,.43,.91),(.44,.10,.99,.91)])
bidder=annotated('bidder-registration.png','bidder-annotated.jpg',[(.01,.08,.29,.92),(.29,.08,.99,.92)])
retail=annotated('retail-auction.png','retail-annotated.jpg',[(.02,.25,.98,.34),(.10,.35,.99,.62),(.01,.62,.99,.84)])
wholesale=annotated('wholesale-auction.png','wholesale-annotated.jpg',[(.02,.27,.98,.36),(.15,.37,.98,.68),(.02,.68,.98,.88)])
cashier=annotated('cashier.png','cashier-annotated.jpg',[(.01,.11,.99,.20),(.01,.65,.99,.79),(.01,.79,.99,.98)])
eon=annotated('end-of-night.png','eon-annotated.jpg',[(.01,.12,.99,.43),(.01,.44,.99,.66),(.01,.67,.99,.99)])
license_screen=annotated('license-activation.png','license-annotated.jpg',[(.05,.25,.83,.58),(.05,.60,.55,.90)])
wholesale_activation=annotated('wholesale-activation.png','wholesale-activation-annotated.jpg',[(.03,.25,.97,.68),(.03,.68,.78,.96)])

story=[]
# Cover
story += [Spacer(1,.55*inch),Image(str(LOGO),width=4.7*inch,height=2.35*inch),Spacer(1,.35*inch),
          p('CUSTOMER USER MANUAL','ManualH1'),p('From registration to final settlement','ManualH2'),Spacer(1,.18*inch),
          data_table([['Product','HammerTrack Pro Auction Management Software'],['Manual version',VERSION],['Applies to','Retail Auction and optional Wholesale Auction module'],['Website','https://hammertrackpro.com']], [1.65*inch,5.37*inch]),Spacer(1,.28*inch),
          callout('Purpose','This manual is written for auction owners, clerks, cashiers, and setup staff who have never used HammerTrack Pro. Follow the chapters in order when preparing your first auction.','tip'),Spacer(1,.45*inch),
          p('Copyright 2026 HammerTrack Pro. Customer training and operational reference.', 'Caption'),PageBreak()]

# TOC
story += [p('Table of Contents','TOCHeading')]
toc=TableOfContents();toc.levelStyles=[styles['TOC1'],styles['TOC2']];story += [toc,PageBreak()]

story += [h1('1. Read This First'),p('HammerTrack Pro runs live auctions from a Windows host computer and can connect a second cashier computer over a private local network. The host owns the auction database. The cashier communicates through HammerTrack Pro and must never open the database file directly.'),
          h2('What the software includes'),*bullets(['Auction setup and duplication','Global seller and buyer records with auction-specific bidder numbers','Retail and optional licensed Wholesale live-clerk screens','Buyer invoices, cash/check/card payments, split payments, refunds, and audited sale corrections','Seller settlements and item summaries','Bidder cards, unpaid-buyer review, backups, and End-of-Night reporting']),
          h2('Current access model'),p(f'Version {VERSION} does <b>not</b> provide individual user accounts, passwords, or configurable permission roles. Access is controlled by the Windows computer account and the workstation role selected during setup: <b>Live Auction Host</b> or <b>Cashier Workstation</b>. Only trusted staff should have Windows access to either computer.'),
          callout('Protect auction data','Keep the host database on the host computer. Do not place it on a shared, mapped, cloud-synchronized, or network drive. Use HammerTrack Pro Backup Database before and after every auction.','important'),PageBreak()]

story += [h1('2. Installing HammerTrack Pro'),h2('Before installation'),*bullets(['Use a supported 64-bit Windows computer.','Have Windows administrator permission available.','For a two-computer setup, connect both computers to the same trusted private router or LAN.','Keep the signed installer and issued .htplicense file available.']),
          h2('Install the program'),*steps([f'Copy <b>HammerTrackPro-{VERSION}-x64-signed.msi</b> to the computer.','Double-click the installer. Approve the Windows administrator prompt.','Read and accept the license agreement.','Choose the installation folder if prompted; the recommended folder is under Program Files.','Finish installation, then open HammerTrack Pro from the Desktop or Start Menu shortcut.']),
          callout('Self-signed installer','If Windows does not yet trust the local HammerTrack Pro certificate, verify that the installer came directly from your HammerTrack Pro provider before continuing. The public certificate may need to be trusted on each computer.','warning'),
          h2('Upgrade an existing installation'),p('Run the newer MSI without uninstalling the older version. The upgrade replaces program files and preserves the database, settings, backups, logs, licenses, and module activations under ProgramData.'),PageBreak()]

story += [h1('3. First-Time Setup and Licensing'),h2('Choose the workstation role'),p('<b>Live Auction Host:</b> stores the local database, runs Retail/Wholesale clerk entry, and hosts the secure cashier connection.<br/><b>Cashier Workstation:</b> connects to the host and runs buyer checkout and seller settlement. It does not open a local shared database.'),
          h2('Activate the main software license'),figure(license_screen,'Figure 3-1. Main license activation: 1 - copy the Machine ID; 2 - import the issued .htplicense file.'),
          *steps(['Click <b>Copy Machine ID</b>.','Send the Machine ID to the HammerTrack Pro license issuer. For two computers, send both IDs together.','Save the issued <b>.htplicense</b> file on the computer.','Click <b>Import License</b>, select the file, and confirm the licensed customer name.','Repeat the import on every Machine ID included in that license.']),
          callout('Keep the private key private','Customers receive only issued license or activation files. Never copy, email, or package the private .pem issuer key.','important'),PageBreak()]

story += [h1('4. Dashboard and Navigation'),figure(dashboard,'Figure 4-1. Dashboard: 1 - Quick Actions; 2 - current-auction settings; 3 - network and backup controls.'),
          h2('Dashboard Quick Actions'),data_table([['Action','Use'],['Register Bidder','Open buyer registration.'],['Add Lot','Open full lot entry.'],['Backup Database','Create a recoverable copy of host data.'],['Refresh Dashboard','Reload current-auction counts and totals.'],['Retail Auction','Open standard live-clerk entry.'],['Wholesale Auction','Open or activate the optional Wholesale module.'],['Manage Sellers','Create, find, edit, and activate sellers.'],['Auction Setup','Create, select, duplicate, or close an auction.'],['Cashier','Open checkout and seller settlement.'],['Help / User Manual','Open this PDF without internet access.']]),
          h2('Dashboard tabs'),p('<b>Dashboard</b> shows the current auction and quick actions. <b>Bidders</b> manages current-auction registrations and global buyer identity details. <b>Lots</b> provides full lot entry and editing. Operational totals and lists always follow the selected current auction.'),PageBreak()]

story += [h1('5. Create and Configure an Auction'),figure(setup,'Figure 5-1. Auction Setup: 1 - auction directory and copy choices; 2 - selected auction details; 3 - Save Auction.'),
          h2('Create a new auction'),*steps(['From the Dashboard, click <b>Auction Setup</b>.','Click <b>New Auction</b>.','Complete Auction Number, Auction Name, date/time, location, staff, and company contact information.','Set Buyer Premium %, Seller Commission %, Sales Tax %, Sales Tax Basis, and card-processing fee terms.','Add invoice and receipt footer text if desired.','Click <b>Save Auction</b>, select it in Saved Auctions, then click <b>Load Selected</b>.']),
          callout('Commercial terms','Premium, sales tax, seller commission, and card fees come from the selected Auction record. Confirm these values before the first sale. Changing terms after payments or payouts begin can create confusing historical results.','important'),
          h2('Duplicate a prior auction'),*steps(['Select the prior auction.','Choose whether to copy sellers and/or registered bidders.','Click <b>Duplicate</b>.','Edit the new Auction Number, name, date, and other details.','Save and load the new auction. Sales and payment history are not copied.']),
          h2('Close an auction'),p('Use Close Auction only after live selling and buyer checkout are complete. Closed auctions remain available for invoice review and seller settlement, but new buyer payments are blocked.'),PageBreak()]

story += [h1('6. Seller Management'),figure(seller,'Figure 6-1. Seller Management: 1 - searchable seller directory; 2 - seller details and actions.'),
          h2('Add a seller'),*steps(['Click <b>Manage Sellers</b>, then <b>Add Seller</b>.','Enter a unique Seller Number and the seller or business name.','Complete optional contact, tax, commission, payout, and notes fields.','Confirm Active is enabled, then click <b>Save Seller</b>.']),
          h2('Seller and bidder number linking'),p('When a seller is saved, HammerTrack Pro creates or links the same-number buyer identity for that auction. If the number already belongs to a different buyer, the save is stopped and you must choose another Seller Number. A seller who buys items can have purchases deducted from settlement proceeds through that durable link.'),
          h2('Commission priority'),p('Settlement calculations use a positive seller commission override first, then the seller default commission, then the selected auction default. Review the effective percentage before recording payout.'),PageBreak()]

story += [h1('7. Register Bidders and Buyers'),figure(bidder,'Figure 7-1. Bidder registration: 1 - bidder details and tax-exempt fields; 2 - current-auction registration list.'),
          h2('Register a bidder'),*steps(['Open the <b>Bidders</b> tab or click <b>Register Bidder</b>.','Enter an unused Bidder Number for the selected auction.','Select an Existing Buyer when the person is already in the global buyer list, or leave Create New Buyer selected.','Enter optional name, address, phone, email, and driver license information.','For a tax-exempt buyer, enter Resale License / Tax Exemption Number, State, and select <b>Tax Exempt</b>.','Click <b>Save</b>.']),
          callout('Bidder numbers are auction-specific','The same person may have a different bidder number in another auction. HammerTrack keeps one durable Buyer identity and a separate registration for each auction.','tip'),
          h2('Scan a driver license'),p('Use <b>Scan ID</b> for the proven legacy PDF417 path. Use <b>Scan New ID</b> for newer cards after configuring the NETUM NT-1228BL to read PDF417 in normal and inverse modes. Review every populated field before saving. The raw barcode and images are not stored.'),
          h2('Tax exemption at checkout'),p('When Tax Exempt is saved on the buyer identity, HammerTrack automatically calculates zero sales tax for that buyer. Other buyers use the selected auction sales-tax rate and basis.'),PageBreak()]

story += [h1('8. Prepare for Auction Day'),h2('Pre-auction checklist'),*bullets(['Load the correct auction and verify its number, name, date, location, and status.','Verify premium, tax basis/rate, card percentage/fixed fee, and seller commission.','Register sellers and bidders; confirm tax-exempt records.','Print bidder cards if used.','Test the scanner, printer, and card terminal. HammerTrack records payments but does not directly process a bank card.','On two computers, verify the Cashier displays Connected to auction host.','Create and safely retain a database backup.','Perform one sample lot, invoice, and print preview before bidders arrive.']),
          h2('Network Information'),p('On the host, open Network Information. The cashier setup uses the host address, pairing key, and certificate fingerprint. If the computer-name address fails, use the Alternate IP shown by the host. Never bypass a fingerprint mismatch.'),PageBreak()]

story += [h1('9. Retail Auction - Standard Sale'),figure(retail,'Figure 9-1. Retail Auction: 1 - create lot; 2 - keyboard sale entry; 3 - recorded sales for verification.'),
          h2('Fast keyboard workflow'),*steps(['Enter the <b>Seller Number</b> and press Tab.','Enter the <b>Lot Description</b> and click <b>Create Lot</b> or press Enter in the description.','Hammer Price receives focus with 0.00 selected. Type the per-item hammer price.','Press Tab. Bidder Number receives focus with 0 selected. Type the bidder number.','Press Tab. Quantity receives focus with 1 selected. Type another quantity or leave 1.','Press Enter to record the sale.']),
          callout('Price is per item','Retail total equals Hammer Price Each x Quantity. Example: 6 items at $5 each records a $30 hammer total.','tip'),
          h2('Verify the sale'),p('The Recorded Sales for Current Lot table shows Lot, Bidder, Quantity, Price Each, and Total. Check the row immediately. Use Undo Last Buyer only for the most recent unpaid sale and confirm the prompt.'),PageBreak()]

story += [h1('10. Retail Pass-Out and Lot Controls'),h2('Use Pass-Out for multiple buyers on one lot'),*steps(['Create the lot and enter Hammer Price Each.','Select <b>PASS-OUT MODE</b>.','Seller, lot, description, and hammer price lock.','Enter Bidder Number, press Tab, enter Quantity, and press Enter.','Repeat for every buyer. After each record, focus returns to Bidder Number.','Click <b>Close Lot</b> when all buyers are recorded.']),
          h2('Other live controls'),data_table([['Control','Behavior'],['Pass Item / Next (F4)','Marks an unsold item Passed and advances.'],['Close Lot (F12)','Closes the current lot deliberately.'],['Undo Last Buyer (F8)','Removes only the newest eligible unpaid sale after confirmation.'],['Fullscreen (F11)','Enters/exits fullscreen; Esc also exits fullscreen.'],['Queued Next Lot','Lets the clerk choose a specific next lot.'],['Auctioneer Notes','Stores current-lot notes.']]),
          callout('Do not advance active Pass-Out accidentally','In Retail Pass-Out, create/advance actions are blocked until Close Lot. This protects the locked lot and price while multiple buyers are entered.','warning'),PageBreak()]

story += [h1('11. Wholesale Module Activation'),figure(wholesale_activation,'Figure 11-1. Wholesale activation: 1 - paste the complete signed key; 2 - Machine ID and activation controls.'),
          h2('Activate Wholesale Auction'),*steps(['Click <b>Wholesale Auction</b> on the Dashboard. The button remains visible even when locked.','Copy the Machine ID from the activation window and send it to the license issuer.','The issuer creates a Wholesale activation key for the authorized Machine ID or IDs.','Open the issued text file and copy the entire key.','Paste it into the activation box and click <b>Activate</b>.','After successful activation, Wholesale Auction opens and remains available offline on that computer.']),
          h2('Issuer command'),p('The license owner runs the following from the project folder, replacing customer and machine values:'),
          callout('PowerShell command',r'<font name="Courier">dotnet run --project ".\HammerTrackPro2.LicenseIssuer\HammerTrackPro.LicenseIssuer.csproj" -c Release -- issue-module --key "$env:LOCALAPPDATA\HammerTrackPro\LicenseIssuer\hammertrack-license-private.pem" --module "Wholesale" --customer "Auction Company" --machines "MACHINE-ID" --output "$env:USERPROFILE\Desktop\Wholesale-activation.txt"</font>','tip'),PageBreak()]

story += [h1('12. Wholesale Auction - Standard Sale'),figure(wholesale,'Figure 12-1. Wholesale Auction: 1 - create lot; 2 - wholesale fields; 3 - detailed deal transaction grid.'),
          h2('Standard wholesale sale'),*steps(['Enter Seller Number and Lot Description, then create the lot.','Enter <b>Hammer Price Each</b>.','Enter Bidder Number.','Enter Quantity, meaning total items purchased in this standard sale.','Leave Pass-Out off. Deals remains 1.','Press Enter to record the buyer.']),
          h2('Standard example'),data_table([['Value','Example'],['Description','Case of glassware'],['Hammer Price Each','$5.00'],['Quantity','24'],['Deals','1'],['Total Quantity','24'],['Sale Total','$120.00']]),PageBreak()]

story += [h1('13. Wholesale Pass-Out Mode'),h2('Meaning of the fields'),data_table([['Field','Wholesale Pass-Out meaning'],['Hammer Price Each','Price for one individual item.'],['Quantity','Number of items inside one deal; locks when Pass-Out starts.'],['Deals','Number of identical deals purchased by the current bidder.'],['Total Quantity','Quantity per Deal x Deals.'],['Sale Total','Hammer Price Each x Total Quantity.']]),
          h2('Fast Pass-Out workflow'),*steps(['Create the wholesale lot.','Enter Hammer Price Each and the Quantity per Deal.','Select <b>PASS-OUT MODE</b>. Seller, lot, description, price, and quantity per deal lock.','The cursor starts at Bidder Number. Type the bidder number.','Press Tab to move to Deals. Type deals purchased.','Press Enter. HammerTrack records that buyer and returns immediately to Bidder Number.','Repeat for each buyer.','Click <b>Close Lot</b> when the lot is finished, or enter a new description and create a new wholesale lot.']),
          h2('Calculation examples'),data_table([['Price Each','Qty / Deal','Deals','Total Qty','Sale Total'],['$5.00','4','6','24','$120.00'],['$2.50','12','3','36','$90.00'],['$10.00','1','8','8','$80.00']], [1.25*inch,1.25*inch,1.0*inch,1.25*inch,2.27*inch]),
          callout('Separate transaction per bidder','Every Pass-Out buyer is stored as an individual transaction with lot, bidder, price each, quantity per deal, deals, total quantity, and sale total. Do not combine different bidders into one entry.','important'),PageBreak()]

story += [h1('14. Correcting a Sale'),h2('Undo during live entry'),p('If the newest sale is wrong and has not been paid, choose <b>Undo Last Buyer (F8)</b>, confirm the displayed transaction, and re-enter it correctly. Only the most recent eligible sale can be undone.'),
          h2('Correct Sale from Cashier'),*steps(['Open Cashier and look up the buyer currently assigned to the sale.','Select the wrong purchase row.','Click <b>Correct Sale</b>.','Enter the required reason.','Choose to remove the sale or replace bidder, quantity, and/or price.','Confirm the audited correction.']),
          callout('Paid-sale protection','A paid sale must be refunded sufficiently before it can be reduced, removed, or moved. The original sale is retained as voided history and the replacement is linked for audit.','important'),PageBreak()]

story += [h1('15. Buyer Checkout'),figure(cashier,'Figure 15-1. Cashier: 1 - buyer actions; 2 - payment entry; 3 - seller settlement controls.'),
          h2('Find and review a buyer'),*steps(['Enter the current auction Bidder Number.','Click <b>Lookup Buyer</b>.','Confirm the buyer name, invoice status, hammer total, premium, tax, card fee, paid amount, and balance.','Review Purchases, Payment History, Pending Split Payments, and Refund History tabs.']),
          h2('Record a cash payment'),*steps(['Select Cash. Invoice Amount automatically fills the unpaid balance.','Enter Cash Tendered. Verify Change Due.','Click <b>Add Payment</b>.','Review the pending line, then click <b>Record Payments</b>.','Return the displayed change and print the invoice.']),
          h2('Record check or card'),p('For Check, enter the applied invoice amount and a reference when needed. For Card, HammerTrack fills the unpaid base amount and previews the current-auction percentage plus fixed processing fee. Process the displayed total on the external card terminal, add the payment, then record it.'),
          callout('Card amount rule','Enter the invoice balance before card fee. HammerTrack adds the configured card fee to the charge and printed invoice. Do not manually add the fee into Invoice Amount.','warning'),PageBreak()]

story += [h1('16. Split Payments, Refunds, and Mistakes'),h2('Split payment'),*steps(['Select the first method and enter only that portion.','Click Add Payment.','Select the second method. HammerTrack suggests the remaining base balance.','Add each portion and review Pending Split Payments.','Remove any incorrect pending row before recording.','Click Record Payments once all portions are correct.']),
          h2('Refund a buyer'),*steps(['Look up the paid buyer.','Click <b>Refund</b>.','Select the original payment.','Enter the refundable purchase amount and a required reason.','Enter an optional reference and confirm.']),
          p('Refunds are append-only records. Card-processing fees are retained and cannot be included in the purchase refund.'),
          h2('Unpaid Buyers'),p('Click Unpaid Buyers to reload a current-auction list of bidder number, name, invoice total, paid amount, and remaining balance. An all-paid message appears when no balances remain.'),PageBreak()]

story += [h1('17. Buyer Invoices, Receipts, and Bidder Cards'),h2('Print an invoice'),*steps(['Look up the bidder.','Confirm purchases and payments.','Click <b>Print Invoice</b>.','Review the professional preview, auction-company information, bidder information, item quantities/prices/totals, premium, tax, card fees, payments, refunds, status, and balance.','Print from the preview toolbar.']),
          h2('Bidder cards'),*steps(['Click <b>Bidder Cards</b>.','Choose a current-auction bidder.','Click Print Preview.','Confirm landscape orientation, complete bidder number, divider, and auction-company name.','Print at 100% scale.']),
          h2('Tax-exempt invoice check'),p('A buyer marked Tax Exempt should show $0.00 sales tax. If tax appears, stop checkout, open bidder registration, correct the buyer record, save it, and look up the invoice again.'),PageBreak()]

story += [h1('18. Seller Settlement'),h2('View seller sales'),*steps(['At the bottom of Cashier, enter Seller Number.','Click <b>Lookup Seller</b>.','Review the settlement heading, Seller Lots, Seller Payouts, gross sales, commission/fees, buyer purchase deduction, net amount, paid amount, and remaining balance.']),
          h2('Record a seller payout'),*steps(['Select Check, Cash, or ACH/manual electronic method.','Enter the payout amount.','For Check or ACH, enter the required reference.','Confirm the remaining balance and any Hold Check setting.','Click <b>Record Seller</b>.']),
          callout('Early payout warning','Seller payouts may be recorded before or after auction close, but later sales or buyer purchases can change the final balance. Final settlement after buyer checkout is safer.','warning'),
          h2('Seller print options'),p('<b>Print Settlement</b> produces the standard financial statement. <b>Print Item Summary</b> groups sold items by lot, description, and price each; shows bidder number, quantities, line totals, Total Items Sold, and Total Hammer Sales.'),PageBreak()]

story += [h1('19. End-of-Night Report'),figure(eon,'Figure 19-1. End-of-Night: 1 - financial summary; 2 - staff/operating payment entry; 3 - audit ledger.'),
          h2('Financial summary'),p('Refresh Totals reloads Hammer Sales, Buyer Premiums, Card Fees, Sales Tax, Invoice Total, Buyer Payments, Still Unpaid, Seller Gross, Seller Commission, Seller Payouts, Staff Payments, and Net Cash Movement for the current auction.'),
          h2('Record staff or operating payments'),*steps(['Choose Category such as Clerk, Auctioneer, Floor Help, or Kitchen Help.','Enter Payee, Amount, Payment Method, and Reference/Notes.','Click Record Payment.','To correct an entry, select it, click Void Selected, and enter the required reason. The original and void remain visible.']),
          h2('Print the report'),p('Click Print Report, inspect the preview, and print. The report includes branded auction-company information, grouped revenue and expense totals, highlighted net cash movement, staff-payment ledger, and signature lines.'),PageBreak()]

story += [h1(f'20. Reports Available in Version {VERSION}'),data_table([['Report or view','Where to open it'],['Dashboard totals','Dashboard; refreshes current-auction bidder, lot, sold, gross, and average values.'],['Buyer invoice/receipt','Cashier > Lookup Buyer > Print Invoice.'],['Unpaid buyer report','Cashier > Unpaid Buyers.'],['Buyer payment/refund history','Cashier tabs after buyer lookup; also printed on invoice.'],['Seller settlement','Cashier > Lookup Seller > Print Settlement.'],['Seller item summary','Cashier > Lookup Seller > Print Item Summary.'],['Seller payout history','Seller Payouts tab and settlement print.'],['Bidder card','Cashier > Bidder Cards.'],['End-of-Night report','Cashier > End-of-Night > Print Report.'],['Audit trail','Operational correction/void history is retained in the database and displayed in relevant ledgers.']]),
          callout('Scope',f'Version {VERSION} does not include a separate custom report designer, user-permission report, inventory-limit report, or direct card-processor settlement import.','tip'),PageBreak()]

story += [h1('21. Backups and Data Protection'),h2('Create a backup'),*steps(['On the host Dashboard, click <b>Backup Database</b>.','Wait for the success message and note the backup path.','Copy a completed backup to a secure external location after HammerTrack closes or after the backup reports success.']),
          h2('Recommended schedule'),*bullets(['Before changing auction settings on auction day.','Immediately before bidding begins.','After major breaks when practical.','After buyer checkout and seller settlement.','Before installing any software upgrade.']),
          h2('What an upgrade preserves'),p('The installer keeps the host database, backups, logs, workstation settings, main license, and Wholesale activation under ProgramData. Never test restore or destructive corrections against the production auction database.'),PageBreak()]

story += [h1('22. Administration and Settings'),h2('Company and auction account information'),p('Company identity is stored with each Auction record: company name, address, city, state, ZIP, phone, and email. Printed invoices and End-of-Night reports use this information. Auction Setup also stores staff names, location, commercial terms, tax basis, card fees, and document footers.'),
          h2('Clerk and cashier access'),p('The host/cashier workstation role controls which operational surface starts on a computer. There are no individual Clerk or Cashier logins in this version. Protect access with Windows accounts, device passwords, physical control, and trusted private networking.'),
          h2('Auction permissions'),p('There is no configurable permission matrix. Staff with access to HammerTrack can use the controls available on that workstation. Establish written procedures for who may close auctions, correct sales, refund buyers, void staff payments, and record seller payouts.'),
          h2('License management'),p('The main .htplicense and Wholesale activation are machine-bound and work offline after installation. Replacing a computer requires a newly issued license/key containing the replacement Machine ID. Keep issuer private keys offline and outside the installer.'),PageBreak()]

story += [h1('23. Troubleshooting'),data_table([['Problem','What to do'],['Program says no license','Copy the displayed Machine ID and import an issued .htplicense containing that exact ID.'],['Wholesale button opens activation','Issue and paste a signed Wholesale key for this PC.'],['Cashier is disconnected','Confirm the host is running, both PCs use the same private network, and host address/pairing key/fingerprint match. Try the Alternate IP.'],['Wrong tax on invoice','Verify selected auction tax rate/basis and the buyer Tax Exempt flag; save and reload the buyer invoice.'],['Card payment exceeds balance','Enter only the invoice balance before fee. Let HammerTrack calculate the fee.'],['Record Payments button appears missing',f'Maximize/fullscreen the Cashier window or scroll horizontally; use the current {VERSION} layout.'],['Scanner beeps but fills nothing','Use PDF417 HID keyboard mode; enable regular and inverse PDF417 for new cards; keep focus in the Scan ID/New ID receiver.'],['Names are incomplete after scan','Confirm the latest software version and review/correct every field before saving.'],['Cannot correct paid sale','Refund enough purchase value first, then use Correct Sale with a reason.'],['Cannot delete buyer/seller/lot','Operational or financial history prevents deletion; preserve the audit record and use the supported correction workflow.'],['Manual button cannot find PDF','Reinstall the current MSI or place HammerTrackPro-User-Manual.pdf beside HammerTrack.exe.'],['Print is clipped','Use Print Preview, correct paper orientation/size, and print at 100% unless the printer requires Fit.']]),PageBreak()]

story += [h1('24. Frequently Asked Questions'),h3('Does HammerTrack process credit cards?'),p('No. It calculates and records the card portion and configured processing fee. Run the displayed charge on your separate card terminal, then record the payment in HammerTrack.'),
          h3('Can two PCs open the database file?'),p('No. The live-auction host alone owns the local database. The cashier connects through the paired HTTPS service.'),
          h3('Can one buyer be registered in many auctions?'),p('Yes. HammerTrack keeps one global Buyer identity and auction-specific bidder registrations.'),
          h3('Can a seller also buy?'),p('Yes. The seller is linked to the same-number Buyer identity when possible, and unpaid purchases can be deducted from settlement proceeds.'),
          h3('What does Quantity mean in Wholesale Pass-Out?'),p('It means items per deal. Deals is how many identical deals the current bidder buys. Total Quantity equals Quantity per Deal x Deals.'),
          h3('Can I refund a card fee?'),p('No. The audited refund workflow refunds purchase funds only and retains card-processing fees.'),
          h3('Does the software have user passwords?'),p(f'Not in version {VERSION}. Use Windows security and workstation roles, and limit physical access to authorized staff.'),
          h3('Where is my data?'),p(r'By default, mutable data is under ProgramData\HammerTrackPro on the host. Use the in-program backup command rather than manually moving a live database.'),PageBreak()]

story += [h1('25. Technical Support and Update Practice'),p('For current contact options, product information, software updates, and support requests, visit <b>https://hammertrackpro.com</b>.'),
          h2('Before requesting support'),*bullets(['Record the software version shown in Apps & Features or installer filename.','Note whether the computer is Host or Cashier.','Write down the exact error message without including private license keys, scanner barcode data, buyer personal data, or payment details.','Describe the last successful action and the steps that reproduce the problem.','Create a database backup when the program can safely do so.']),
          h2('Keeping this manual current'),p(f'This PDF is packaged with version {VERSION} and opens from Help / User Manual. When major screens or workflows change, update the screenshots, directions, manual version, installer source, and automated Help-button test together. Replace the installed manual only through a tested software upgrade so the instructions match the program.'),
          callout('Auction-day emergency rule','Protect the database and written audit trail first. Do not delete files, bypass license/network security, or edit the SQLite database manually to force an auction-day correction.','important'),Spacer(1,.2*inch),
          p('End of HammerTrack Pro Customer User Manual','Caption')]

OUT.parent.mkdir(parents=True,exist_ok=True)
doc=ManualDocTemplate(str(OUT));doc.multiBuild(story)
print(OUT)
