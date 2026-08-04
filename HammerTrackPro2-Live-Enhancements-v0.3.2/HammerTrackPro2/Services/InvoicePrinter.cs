using System.Drawing.Printing;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class InvoicePrinter
{
    public PrintDocument CreateDocument(Invoice invoice, Auction auction)
    {
        ArgumentNullException.ThrowIfNull(invoice); ArgumentNullException.ThrowIfNull(auction);
        var document=new PrintDocument { DocumentName=$"Invoice {invoice.FormattedInvoiceNumber}" };
        var nextLine=0;
        document.PrintPage += (_,e) =>
        {
            nextLine=DrawInvoice(e.Graphics ?? throw new InvalidOperationException("The printer did not provide a drawing surface."),e.MarginBounds,invoice,auction,nextLine);
            e.HasMorePages=nextLine<invoice.Purchases.Count;
            if(!e.HasMorePages) nextLine=0;
        };
        return document;
    }

    internal static int DrawInvoice(Graphics graphics,Rectangle bounds,Invoice invoice,Auction auction,int startLine=0)
    {
        using var titleFont=new Font("Segoe UI",18,FontStyle.Bold); using var headingFont=new Font("Segoe UI",10,FontStyle.Bold);
        using var bodyFont=new Font("Segoe UI",9); using var linePen=new Pen(Color.Black,1);
        var y=bounds.Top; graphics.DrawString(auction.AuctionName,titleFont,Brushes.Black,bounds.Left,y); y+=34;
        graphics.DrawString($"INVOICE {invoice.FormattedInvoiceNumber}",headingFont,Brushes.Black,bounds.Left,y);
        graphics.DrawString($"Status: {invoice.Status}",headingFont,Brushes.Black,bounds.Right-150,y); y+=24;
        graphics.DrawString($"Auction: {auction.AuctionNumber}   Date: {auction.AuctionDate:d}   Bidder: {invoice.BidderNumber}",bodyFont,Brushes.Black,bounds.Left,y); y+=20;
        graphics.DrawString($"Buyer: {invoice.BuyerName}",bodyFont,Brushes.Black,bounds.Left,y); y+=18;
        graphics.DrawString(invoice.BuyerAddress,bodyFont,Brushes.Black,bounds.Left,y); y+=28;
        graphics.DrawLine(linePen,bounds.Left,y,bounds.Right,y); y+=8;
        graphics.DrawString("Lot",headingFont,Brushes.Black,bounds.Left,y); graphics.DrawString("Description",headingFont,Brushes.Black,bounds.Left+60,y);
        graphics.DrawString("Hammer",headingFont,Brushes.Black,bounds.Right-90,y); y+=20;
        var availableRows=Math.Max(1,(bounds.Bottom-y-230)/18);
        var endLine=Math.Min(invoice.Purchases.Count,startLine+availableRows);
        for(var index=startLine;index<endLine;index++)
        {
            var line=invoice.Purchases[index];
            graphics.DrawString(line.LotNumber.ToString(),bodyFont,Brushes.Black,bounds.Left,y);
            graphics.DrawString(line.Description,bodyFont,Brushes.Black,new RectangleF(bounds.Left+60,y,bounds.Width-170,18));
            graphics.DrawString(line.HammerPrice.ToString("C2"),bodyFont,Brushes.Black,bounds.Right-90,y); y+=18;
        }
        if(endLine<invoice.Purchases.Count)
        {
            graphics.DrawString($"Continued — {invoice.Purchases.Count-endLine} purchase(s) remain",bodyFont,Brushes.Black,bounds.Left,y+12);
            return endLine;
        }
        y+=6; graphics.DrawLine(linePen,bounds.Left,y,bounds.Right,y); y+=10;
        DrawTotal(graphics,bodyFont,bounds,ref y,"Hammer subtotal",invoice.HammerSubtotal);
        DrawTotal(graphics,bodyFont,bounds,ref y,$"Buyer's premium ({invoice.BuyerPremiumPercent:0.##}%)",invoice.BuyerPremiumAmount);
        DrawTotal(graphics,bodyFont,bounds,ref y,$"Sales tax ({invoice.SalesTaxPercent:0.##}%)",invoice.SalesTaxAmount);
        DrawTotal(graphics,bodyFont,bounds,ref y,"Credit-card processing fees",invoice.CreditCardFeeAmount);
        DrawTotal(graphics,headingFont,bounds,ref y,"Invoice total",invoice.TotalAmount);
        DrawTotal(graphics,bodyFont,bounds,ref y,"Payments",invoice.AmountPaid);
        DrawTotal(graphics,headingFont,bounds,ref y,"Balance due",invoice.BalanceDue);
        y+=12; graphics.DrawString(auction.InvoiceFooter ?? string.Empty,bodyFont,Brushes.Black,new RectangleF(bounds.Left,y,bounds.Width,60));
        return endLine;
    }

    private static void DrawTotal(Graphics graphics,Font font,Rectangle bounds,ref int y,string label,decimal amount)
    {
        graphics.DrawString(label,font,Brushes.Black,bounds.Right-270,y); graphics.DrawString(amount.ToString("C2"),font,Brushes.Black,bounds.Right-90,y); y+=20;
    }
}
