using System.Text;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class HandheldLicenseScanViewModel
{
    public const int InitialCompletionDelayMilliseconds=550;
    public const int AdditionalChunkDelayMilliseconds=1800;
    private readonly AamvaDriverLicenseParser _parser;
    private readonly StringBuilder _buffer=new();
    public HandheldLicenseScanViewModel(AamvaDriverLicenseParser? parser=null)=>_parser=parser??new AamvaDriverLicenseParser();
    public DriverLicenseScanData? Result { get; private set; }
    public int CharacterCount=>_buffer.Length;
    public string Status { get; private set; }="Ready — pull the scanner trigger";

    public void Receive(char character)
    {
        if(character=='\0')return;
        if(character is '\r' or '\n' or '\t' or '\f'){if(_buffer.Length==0||_buffer[^1]!='\n')_buffer.Append('\n');}
        else if(!char.IsControl(character)||character is '\u001d' or '\u001e')_buffer.Append(character);
        Status=$"Receiving license barcode… {_buffer.Length} characters";
    }

    public void Receive(string characters)
    {
        foreach(var character in characters)Receive(character);
    }

    public bool Complete(bool discardOnFailure=true)
    {
        if(_buffer.Length==0){Status="No scanner data received. Confirm USB HID/keyboard mode and try again.";return false;}
        try{Result=_parser.Parse(_buffer.ToString());Status="Driver's license scanned";return true;}
        catch(InvalidOperationException exception)
        {
            Result=null;
            if(!discardOnFailure){Status=$"Receiving license barcodeâ€¦ {_buffer.Length} characters. Waiting for the remaining scanner data.";return false;}
            Status=$"Scan was not recognized: {exception.Message} Check that PDF417 is enabled and scan again.";_buffer.Clear();return false;
        }
    }

    public void Reset(){_buffer.Clear();Result=null;Status="Ready — pull the scanner trigger";}
}
