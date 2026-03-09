namespace PAP3N_DOTNET;

public class Example
{
    public static void Main(string[] args)
    {
        var exitEvent = new ManualResetEvent(false);
        Console.CancelKeyPress += (sender, eventArgs) => {
            eventArgs.Cancel = true; // Prevent immediate termination
            exitEvent.Set();        // Signal the application to exit gracefully
        };
          
        var device = PAP3N.getDevices().First();
        var mcp = new PAP3N(device);
        mcp.connect();

        mcp.B_BNK_SEL_10.onChange += b => {if (b) {Console.WriteLine("10");}};
        mcp.B_BNK_SEL_15.onChange += b => {if (b) {Console.WriteLine("15");}};
        mcp.B_BNK_SEL_20.onChange += b => {if (b) {Console.WriteLine("20");}};
        mcp.B_BNK_SEL_25.onChange += b => {if (b) {Console.WriteLine("25");}};
        mcp.B_BNK_SEL_30.onChange += b => {if (b) {Console.WriteLine("30");}};
        
        bool N1 = false, SPEED = false, LVL_CHANGE = false, VNAV = false;
        mcp.B_N1.onChange += b => mcp.N1.setVal(N1 ^= b); 
        mcp.B_SPEED.onChange += b => mcp.SPEED.setVal(SPEED ^= b); 
        mcp.B_LVL_CHG.onChange += b => mcp.LVL_CHG.setVal(LVL_CHANGE ^= b); 
        mcp.B_VNAV.onChange += b => mcp.VNAV.setVal(VNAV ^= b);
        mcp.KNOB_CRS_R.onChange += v =>
        {
            var val = (byte)(25 * (v % 10));
            mcp.setPanelBrightness(val);
            mcp.setDigitBrightness(val);
            mcp.setButtonBrightness(val);
        };
        mcp.KNOB_ALT.onChange += v => mcp.ALT_DISPLAY.setString((1000 * v + v).ToString()); 
        mcp.KNOB_CRS_L.onChange += v => Console.WriteLine("CRS_L: " + v); 
        mcp.KNOB_CRS_L.onChange += v =>
        {
            mcp.CRS_L_DISPLAY.setString(v.ToString());
        }; 
        mcp.KNOB_IAS_MACH.onChange += v =>
        {
            if (v % 3 == 0) mcp.IAS_DISPLAY.setString("A." + v);
            else if (v % 3 == 1) mcp.IAS_DISPLAY.setString("B." + v);
            else mcp.IAS_DISPLAY.setString("0." + v);
        };
        mcp.KNOB_VS.onChange += v =>
        {
            var val = (v % 16 - 8) * 500;
            string front = "";
            if (val > 0) front = "+";
            if (val < 0) front = "-";
            mcp.VS_DISPLAY.setString(front + Math.Abs(val));
        };
          
        exitEvent.WaitOne();
    }
}