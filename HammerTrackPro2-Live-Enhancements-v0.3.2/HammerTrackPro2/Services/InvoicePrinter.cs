using System.Drawing.Printing;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class InvoicePrinter
{
    internal const string DocumentHeading="INVOICE / RECEIPT";
    internal const string WarrantyDisclaimer="""
Condition of Sale – Disclaimer of Warranties

All items are offered for sale "AS IS" and "WHERE IS," with no representations or warranties, express or implied, regarding their authenticity, condition, merchantability, fitness for a particular purpose, or value. Buyers are responsible for inspecting items prior to bidding and assume all risks associated with their purchase.

Any guarantees or warranties provided are solely between the buyer and the seller. The auction company acts only as a facilitator of the sale and shall not be responsible or liable for any representations, warranties, guarantees, disputes, returns, exchanges, refunds, or claims arising from the transaction. By participating in the auction, buyers agree to hold the auction company harmless from any such claims or liabilities.
""";
    public PrintDocument CreateDocument(Invoice invoice,Auction auction)
    {
        ArgumentNullException.ThrowIfNull(invoice);ArgumentNullException.ThrowIfNull(auction);var document=new PrintDocument{DocumentName=$"Invoice {invoice.FormattedInvoiceNumber}"};var nextLine=0;
        document.PrintPage+=(_,e)=>{nextLine=DrawInvoice(e.Graphics??throw new InvalidOperationException("The printer did not provide a drawing surface."),e.MarginBounds,invoice,auction,nextLine);e.HasMorePages=nextLine<invoice.Purchases.Count;if(!e.HasMorePages)nextLine=0;};return document;
    }
    internal static int DrawInvoice(Graphics graphics,Rectangle bounds,Invoice invoice,Auction auction,int startLine=0)
    {
        using var heading=new Font("Segoe UI",9,FontStyle.Bold);using var body=new Font("Segoe UI",8.5F);using var small=new Font("Segoe UI",7.3F);using var total=new Font("Segoe UI",10,FontStyle.Bold);using var line=new Pen(Color.FromArgb(190,200,210));using var navyBrush=new SolidBrush(PrintBranding.Navy);using var whiteBrush=new SolidBrush(Color.White);using var right=new StringFormat{Alignment=StringAlignment.Far};
        var y=PrintBranding.DrawHeader(graphics,bounds,auction,DocumentHeading,$"No. {invoice.FormattedInvoiceNumber}");
        graphics.DrawString($"Auction: {auction.AuctionNumber} - {auction.AuctionName}",heading,navyBrush,bounds.Left,y);graphics.DrawString($"Date: {auction.AuctionDate:d}",body,Brushes.Black,bounds.Left,y+20);graphics.DrawString($"Location: {auction.Location}",body,Brushes.Black,bounds.Left+150,y+20);graphics.DrawString($"STATUS: {invoice.Status.ToUpperInvariant()}",heading,navyBrush,new RectangleF(bounds.Right-190,y,190,20),right);y+=48;
        PrintBranding.DrawSectionTitle(graphics,"BIDDER INFORMATION",new Rectangle(bounds.Left,y,bounds.Width,26));y+=34;
        graphics.DrawString($"Bidder #{invoice.BidderNumber}",heading,navyBrush,bounds.Left,y);graphics.DrawString(invoice.BuyerName,heading,Brushes.Black,bounds.Left+105,y);y+=18;
        if(!string.IsNullOrWhiteSpace(invoice.BuyerAddress)){graphics.DrawString(invoice.BuyerAddress,body,Brushes.Black,bounds.Left,y);y+=16;}
        var buyerContact=string.Join("  |  ",new[]{invoice.BuyerPhone,invoice.BuyerEmail}.Where(value=>!string.IsNullOrWhiteSpace(value)));if(!string.IsNullOrWhiteSpace(buyerContact)){graphics.DrawString(buyerContact,body,Brushes.Black,bounds.Left,y);y+=16;}y+=5;
        var quantityX=bounds.Right-245;var priceX=bounds.Right-175;var totalX=bounds.Right-90;
        graphics.FillRectangle(navyBrush,bounds.Left,y,bounds.Width,26);graphics.DrawString("ITEM",heading,whiteBrush,bounds.Left+8,y+5);graphics.DrawString("QUANTITY",heading,whiteBrush,new RectangleF(quantityX,y+5,65,18),right);graphics.DrawString("PRICE",heading,whiteBrush,new RectangleF(priceX,y+5,75,18),right);graphics.DrawString("TOTAL",heading,whiteBrush,new RectangleF(totalX,y+5,80,18),right);y+=30;
        var reserve=385+Math.Min(invoice.Payments.Count,5)*17;var availableRows=Math.Max(1,(bounds.Bottom-y-reserve)/20);var endLine=Math.Min(invoice.Purchases.Count,startLine+availableRows);
        for(var index=startLine;index<endLine;index++){var purchase=invoice.Purchases[index];if((index-startLine)%2==1)graphics.FillRectangle(Brushes.WhiteSmoke,bounds.Left,y-1,bounds.Width,20);graphics.DrawString($"Lot {purchase.LotNumber} - {purchase.Description}",body,Brushes.Black,new RectangleF(bounds.Left+8,y,quantityX-bounds.Left-16,18));graphics.DrawString(purchase.Quantity.ToString(),body,Brushes.Black,new RectangleF(quantityX,y,65,18),right);graphics.DrawString(purchase.HammerPrice.ToString("C2"),body,Brushes.Black,new RectangleF(priceX,y,75,18),right);graphics.DrawString(purchase.LineTotal.ToString("C2"),body,Brushes.Black,new RectangleF(totalX,y,80,18),right);y+=20;}
        if(endLine<invoice.Purchases.Count){graphics.DrawLine(line,bounds.Left,y,bounds.Right,y);graphics.DrawString($"Continued - {invoice.Purchases.Count-endLine} purchase(s) remain",heading,navyBrush,bounds.Left,y+12);return endLine;}
        graphics.DrawLine(line,bounds.Left,y,bounds.Right,y);y+=14;var detailTop=y;
        PrintBranding.DrawSectionTitle(graphics,"PAYMENT HISTORY",new Rectangle(bounds.Left,y,bounds.Width/2-12,26));y+=32;
        if(invoice.Payments.Count==0){graphics.DrawString("No recorded payments.",body,Brushes.Gray,bounds.Left+8,y);y+=18;}else foreach(var payment in invoice.Payments.Take(5)){var reference=string.IsNullOrWhiteSpace(payment.Reference)?string.Empty:$"  Ref: {payment.Reference}";graphics.DrawString($"{payment.CreatedUtc.ToLocalTime():g}  {payment.Method}{reference}",small,Brushes.Black,bounds.Left+8,y);graphics.DrawString(payment.TenderedAmount.ToString("C2"),body,Brushes.Black,new RectangleF(bounds.Left+bounds.Width/2-105,y,85,17),right);y+=17;}
        if(invoice.Payments.Count>5){graphics.DrawString($"+ {invoice.Payments.Count-5} additional payment record(s)",small,Brushes.Gray,bounds.Left+8,y);y+=17;}
        var totalsX=bounds.Left+bounds.Width/2+8;var totalsWidth=bounds.Width/2-8;PrintBranding.DrawSectionTitle(graphics,"INVOICE TOTALS",new Rectangle(totalsX,detailTop,totalsWidth,26));var ty=detailTop+33;
        DrawTotal(graphics,body,bounds,totalsX,totalsWidth,ref ty,"Hammer subtotal",invoice.HammerSubtotal);DrawTotal(graphics,body,bounds,totalsX,totalsWidth,ref ty,$"Buyer's premium ({invoice.BuyerPremiumPercent:0.##}%)",invoice.BuyerPremiumAmount);DrawTotal(graphics,body,bounds,totalsX,totalsWidth,ref ty,$"Sales tax ({invoice.SalesTaxPercent:0.##}%)",invoice.SalesTaxAmount);DrawTotal(graphics,body,bounds,totalsX,totalsWidth,ref ty,"Card processing fees",invoice.CreditCardFeeAmount);DrawTotal(graphics,total,bounds,totalsX,totalsWidth,ref ty,"INVOICE TOTAL",invoice.TotalAmount);DrawTotal(graphics,body,bounds,totalsX,totalsWidth,ref ty,"Payments",invoice.AmountPaid);DrawTotal(graphics,total,bounds,totalsX,totalsWidth,ref ty,"BALANCE DUE",invoice.BalanceDue);
        y=Math.Max(y,ty)+10;var footer=invoice.Status.Equals("Paid",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(auction.ReceiptFooter)?auction.ReceiptFooter:auction.InvoiceFooter;if(!string.IsNullOrWhiteSpace(footer)){using var footerBrush=new SolidBrush(Color.FromArgb(255,247,224));graphics.FillRectangle(footerBrush,bounds.Left,y,bounds.Width,34);graphics.DrawString(footer,body,Brushes.Black,new RectangleF(bounds.Left+8,y+7,bounds.Width-16,24));y+=42;}
        var paragraphs=WarrantyDisclaimer.Split("\n\n");graphics.DrawString(paragraphs[0],heading,navyBrush,bounds.Left,y);y+=18;graphics.DrawString(paragraphs[1],small,Brushes.Black,new RectangleF(bounds.Left,y,bounds.Width,62));y+=66;graphics.DrawString(paragraphs[2],small,Brushes.Black,new RectangleF(bounds.Left,y,bounds.Width,78));
        using var footerFont=new Font("Segoe UI",7);graphics.DrawString("Generated by HammerTrack Pro",footerFont,Brushes.Gray,bounds.Left,bounds.Bottom-14);graphics.DrawString($"Printed {DateTime.Now:g}",footerFont,Brushes.Gray,new RectangleF(bounds.Right-180,bounds.Bottom-14,180,14),right);return endLine;
    }
    private static void DrawTotal(Graphics graphics,Font font,Rectangle bounds,int x,int width,ref int y,string label,decimal amount){using var right=new StringFormat{Alignment=StringAlignment.Far};graphics.DrawString(label,font,Brushes.Black,x+8,y);graphics.DrawString(amount.ToString("C2"),font,Brushes.Black,new RectangleF(x+width-105,y,95,18),right);y+=19;}
}
