using System.Reflection;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

internal static class PrintBranding
{
    internal static readonly Color Navy=Color.FromArgb(24,50,74);
    internal static readonly Color Blue=Color.FromArgb(24,96,150);
    internal static readonly Color Gold=Color.FromArgb(224,168,54);
    internal static Image LoadLogo()
    {
        var assembly=Assembly.GetExecutingAssembly();
        var resource=assembly.GetManifestResourceNames().Single(name=>name.EndsWith("Assets.HammerTrackPro.png",StringComparison.Ordinal));
        using var stream=assembly.GetManifestResourceStream(resource)??throw new InvalidOperationException("HammerTrack Pro print logo is unavailable.");
        using var source=Image.FromStream(stream);return new Bitmap(source);
    }
    internal static int DrawHeader(Graphics graphics,Rectangle bounds,Auction auction,string documentTitle,string documentNumber)
    {
        using var logo=LoadLogo();using var companyFont=new Font("Segoe UI",13,FontStyle.Bold);using var body=new Font("Segoe UI",8.5F);using var title=new Font("Segoe UI",18,FontStyle.Bold);using var number=new Font("Segoe UI",10,FontStyle.Bold);using var line=new Pen(Navy,3);using var right=new StringFormat{Alignment=StringAlignment.Far};
        var top=bounds.Top;graphics.DrawImage(logo,new Rectangle(bounds.Left,top,66,66));var companyX=bounds.Left+78;
        graphics.DrawString(auction.CompanyDisplayName,companyFont,new SolidBrush(Navy),companyX,top);
        var y=top+24;if(!string.IsNullOrWhiteSpace(auction.CompanyFormattedAddress)){graphics.DrawString(auction.CompanyFormattedAddress,body,Brushes.Black,companyX,y);y+=16;}
        var contact=string.Join("  |  ",new[]{auction.CompanyPhone,auction.CompanyEmail}.Where(value=>!string.IsNullOrWhiteSpace(value)));if(!string.IsNullOrWhiteSpace(contact))graphics.DrawString(contact,body,Brushes.Black,companyX,y);
        graphics.DrawString(documentTitle,title,new SolidBrush(Navy),new RectangleF(bounds.Right-245,top,245,30),right);graphics.DrawString(documentNumber,number,Brushes.Black,new RectangleF(bounds.Right-245,top+34,245,22),right);
        graphics.DrawLine(line,bounds.Left,top+76,bounds.Right,top+76);return top+88;
    }
    internal static void DrawSectionTitle(Graphics graphics,string text,Rectangle rectangle)
    {
        using var font=new Font("Segoe UI",9,FontStyle.Bold);using var brush=new SolidBrush(Navy);using var fill=new SolidBrush(Color.FromArgb(232,239,245));graphics.FillRectangle(fill,rectangle);graphics.DrawString(text,font,brush,rectangle.Left+8,rectangle.Top+5);
    }
}
