using HidSharp;

public class PAP3N
{
     private string lastReport = "";
     private const int PAP3_PRODUCT_ID = 0xbf0f;

     
     public class Out(byte[] ON, byte[] OFF, Action<byte[]>? hidOut = null)
     {
          public byte[] on = ON;
          public byte[] off = OFF;
          private bool? val;

          public void setVal(bool newVal)
          {
               if (newVal != val)
               {
                    if (newVal)
                         hidOut?.Invoke(on);
                    else
                         hidOut?.Invoke(off);

                    val = newVal;
               }
          }
     }

     public Out N1;
     public Out SPEED;
     public Out VNAV;
     public Out LVL_CHG;
     public Out HDG_SEL;
     public Out LNAV;
     public Out VOR_LOC;
     public Out APP;
     public Out ALT_HOLD;
     public Out VS;
     public Out CMDA;
     public Out CWSA;
     public Out CMDB;
     public Out CWSB;
     public Out AT_ARM;
     public Out MASTER_L;
     public Out MASTER_R;
     public Out SOLENOID;
     public Out[] LEDS;

     public abstract class SegmentedDisplay()
     {
          public abstract void writeCharacter(byte[] data, int offset, char c);
          
          private void packSegment(byte[] data, int offset, bool on, byte[] segment)
          {
               // Start with the original data
               int byteIndex = offset + segment[0];
               int bitIndex = segment[1];

               byte val = data[byteIndex];
               if (on)
               {
                    // Turn on the bit corresponding to the segment
                    val |= (byte)(1 << (7 - bitIndex));
               }
               else
               {
                    // Turn off the bit corresponding to the segment
                    val &= (byte)~(1 << (7 - bitIndex));
               }
               
               data[byteIndex] = val;
          }
          
          protected void turnOn(byte[] data, int offset, params byte[]?[] segments)
          {
               foreach (var segment in segments)
               {
                    if (segment != null) packSegment(data, offset, true, segment);
               }
          }
          
          protected void turnOff(byte[] data, int offset, params byte[]?[] segments)
          {
               foreach (var segment in segments)
               {
                    if (segment != null) packSegment(data, offset, false, segment);
               }
          }
     }
     
     // Numbering scheme is [byte number][bit number], left to right as if indexing into a bitfield
     class SevenSegmentDisplay(byte[] A, byte[] B, byte[] C, byte[] D, byte[] E, byte[] F, byte[] G, byte[]? DP = null) : SegmentedDisplay()
     {
          public override void writeCharacter(byte[] data, int offset, char c)
          {
               switch (c)
               {
                    case ' ': turnOff(data, offset, A, B, C, D, E, F, G, DP); break;
                    case 'A': turnOn(data, offset, A, B, C, E, F, G); break;
                    case 'B': turnOn(data, offset, A, B, C, D, E, F, G); break;
                    case '0': turnOn(data, offset, A, B, C, D, E, F); break;
                    case '1': turnOn(data, offset, B, C); break;
                    case '2': turnOn(data, offset, A, B, D, E, G); break;
                    case '3': turnOn(data, offset, A, B, C, D, G); break;
                    case '4': turnOn(data, offset,  B, C, G, F); break;
                    case '5': turnOn(data, offset, A, C, D, F, G); break;
                    case '6': turnOn(data, offset, A, C, D, E, F, G); break;
                    case '7': turnOn(data, offset, A, B, C); break;
                    case '8': turnOn(data, offset, A, B, C, D, E, F, G); break;
                    case '9': turnOn(data, offset, A, B, C, D, F, G); break;
                    case '.':
                         if (DP != null)
                              turnOn(data, offset, DP!);
                         break;
               }
          }
     }

     class NineSegmentDisplay(byte[] A, byte[] B, byte[] C, byte[] D, byte[] E, byte[] F, byte[] G, byte[] H, byte[] I)
          : SevenSegmentDisplay(A, B, C, D, E, F, G)
     {
          public override void writeCharacter(byte[] data, int offset, char c)
          {
               switch (c)
               {
                    // Lol, so much for a nine-segment display...
                    case ' ': turnOff(data, offset, A, B, C, D, E, F, G, H, I); break;
                    case 'B': turnOn(data, offset, A, B, C, D, E, F, G, H, I); break;
                    default: base.writeCharacter(data, offset, c); break;
               }
          } 
     }

     class ThreeSegmentDisplay(byte[] A, byte[] B, byte[] C) : SegmentedDisplay()
     {
          public override void writeCharacter(byte[] data, int offset, char c)
          {
               switch (c)
               {
                    case ' ': turnOff(data, offset, A, B, C); break;
                    case '+': turnOn(data, offset, A, B, C); break;
                    case '-': turnOn(data, offset, C); break;
               }
          } 
     }

     class Marker(byte[] Q, byte[]? P = null) : SegmentedDisplay()
     {
          public override void writeCharacter(byte[] data, int offset, char c)
          {
               switch (c)
               {
                    case ' ': turnOff(data, offset, Q, P); break;
                    default: turnOn(data, offset, Q, P); break;
               }          
          }
     }
     
     public class CompositeDisplay(Action sendOutputReport, params SegmentedDisplay[] displays)
     {
          private string s = "";

          public void setString(string newString)
          {
               s = newString;
               // Spit it out
               sendOutputReport.Invoke();
          }
          
          public void writeString(byte[] data, int offset)
          {
               var chars = s.ToCharArray();
               var j = chars.Length - 1;
               foreach (var display in displays)
               {
                    // Clear
                    display.writeCharacter(data, offset, ' ');
               }

               // Print
               for (var i = 0; i < displays.Length; i++)
               {
                    var display = displays[i];
                    if (j >= 0)
                    {
                         display.writeCharacter(data, offset, chars[j]);
                         // Keep the same display to display the digit as well as the dot.
                         if (chars[j] == '.')
                              i--;

                         j--;
                    }
               }
          }
     }

     private SevenSegmentDisplay CRS_R_DIGIT_0 = new([31, 3], [27, 3], [23, 3], [19, 3], [15, 3], [11, 3], [7, 3]);
     private SevenSegmentDisplay CRS_R_DIGIT_1 = new([31, 2], [27, 2], [23, 2], [19, 2], [15, 2], [11, 2], [7, 2]);
     private SevenSegmentDisplay CRS_R_DIGIT_2 = new([31, 1], [27, 1], [23, 1], [19, 1], [15, 1], [11, 1], [7, 1]);
     public CompositeDisplay CRS_R_DISPLAY;
     
     private SevenSegmentDisplay VS_DIGIT_0 = new([30, 7], [26, 7], [22, 7], [18, 7], [14, 7], [10, 7], [6, 7]);
     private SevenSegmentDisplay VS_DIGIT_1 = new([30, 6], [26, 6], [22, 6], [18, 6], [14, 6], [10, 6], [6, 6]);
     private SevenSegmentDisplay VS_DIGIT_2 = new([30, 5], [26, 5], [22, 5], [18, 5], [14, 5], [10, 5], [6, 5]);
     private SevenSegmentDisplay VS_DIGIT_3 = new([30, 4], [26, 4], [22, 4], [18, 4], [14, 4], [10, 4], [6, 4]);
     private ThreeSegmentDisplay VS_PLUS_MINUS = new(/*|*/ [19, 0], /*|*/ [15, 0], /*-*/ [6, 3]);
     public CompositeDisplay VS_DISPLAY;
     
     private SevenSegmentDisplay ALT_DIGIT_0 = new([30, 1], [26, 1], [22, 1], [18, 1], [14, 1], [10, 1], [6, 1]);
     private SevenSegmentDisplay ALT_DIGIT_1 = new([30, 0], [26, 0], [22, 0], [18, 0], [14, 0], [10, 0], [6, 0]);
     private SevenSegmentDisplay ALT_DIGIT_2 = new([29, 7], [25, 7], [21, 7], [17, 7], [13, 7], [9, 7], [5, 7]);
     private SevenSegmentDisplay ALT_DIGIT_3 = new([29, 6], [25, 6], [21, 6], [17, 6], [13, 6], [9, 6], [5, 6]);
     private SevenSegmentDisplay AlT_DIGIT_4 = new([29, 5], [25, 5], [21, 5], [17, 5], [13, 5], [9, 5], [5, 5]);
     public CompositeDisplay ALT_DISPLAY;

     private SevenSegmentDisplay HDG_DIGIT_0 = new([29, 3], [25, 3], [21, 3], [17, 3], [13, 3], [9, 3], [5, 3]);
     private SevenSegmentDisplay HDG_DIGIT_1 = new([29, 2], [25, 2], [21, 2], [17, 2], [13, 2], [9, 2], [5, 2]);
     private SevenSegmentDisplay HDG_DIGIT_2 = new([29, 1], [25, 1], [21, 1], [17, 1], [13, 1], [9, 1], [5, 1]);
     private CompositeDisplay HDG_DISPLAY;
     private Marker HDG_MARKER = new( [29, 4], [25, 4]);
     private Marker TRK_MARKER = new([21, 4], [17, 4]);
     
     private SevenSegmentDisplay IAS_DIGIT_0 = new([28, 7], [24, 7], [20, 7], [16, 7], [12, 7], [8, 7], [4, 7]);
     private SevenSegmentDisplay IAS_DIGIT_1 = new([28, 6], [24, 6], [20, 6], [16, 6], [12, 6], [8, 6], [4, 6]);
     private SevenSegmentDisplay IAS_DIGIT_2 = new([28, 5], [24, 5], [20, 5], [16, 5], [12, 5], [8, 5], [4, 5],[0, 5]);
     private NineSegmentDisplay IAS_DIGIT_3 = new([28, 4], [24, 4], [20, 4], [16, 4], [12, 4], [8, 4], [4, 4], [9, 0], [5, 0]);
     public CompositeDisplay IAS_DISPLAY;
     private Marker IAS_MARKER = new([29, 0]); // TODO check 33,0....
     private Marker MACH_MARKER = new([25, 0], [21, 0]);

     
     private SevenSegmentDisplay CRS_L_DIGIT_0 = new([28, 2], [24, 2], [20, 2], [16, 2], [12, 2], [8, 2], [4, 2]);
     private SevenSegmentDisplay CRS_L_DIGIT_1 = new([28, 1], [24, 1], [20, 1], [16, 1], [12, 1], [8, 1], [4, 1]);
     private SevenSegmentDisplay CRS_L_DIGIT_2 = new([28, 0], [24, 0], [20, 0], [16, 0], [12, 0], [8, 0], [4, 0]);
     public CompositeDisplay CRS_L_DISPLAY;
     
     /*             Marker?  Buttons  ...... counters.... etc
      * Report bytes: 01-    00-00-00-  D0-02-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-01-00-4E-FF-F9-FF-D8-FC-FD-FF-FF-FF-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
      */
     public record Button(int byteOffset, int bitOffset)
     {
          public event Action<bool>? onChange;
          private bool? val;
          
          public void setVal(bool newVal)
          {
               if (val != newVal)
               {
                    val = newVal;
                    onChange?.Invoke(newVal);
               }
          }
     }

     public Button B_N1 = new(1, 7);
     public Button B_SPEED = new(1, 6);
     public Button B_VNAV = new(1, 5);
     public Button B_LVL_CHG = new(1, 4);
     public Button B_HDG_SEL = new(1, 3);
     public Button B_LNAV = new(1, 2);
     public Button B_VOR_LOC = new(1, 1);
     public Button B_APP = new(1, 0);
     public Button B_ALT_HOLD = new(2, 7);
     public Button B_VS = new(2, 6);
     public Button B_CMD_1 = new(2, 5);
     public Button B_CWS_1 = new(2, 4);
     public Button B_CMD_2 = new(2, 3);
     public Button B_CSW_2 = new(2, 2);
     public Button B_IAS_MACH_CO = new(2, 1);
     public Button B_SPD_INTV = new(2, 0);
     public Button B_ALT_INTV = new(3, 7);
     public Button B_L_FD_ON = new(4, 3);
     public Button B_AT_ARM_ON = new(6, 1);
     public Button B_AP_DISENGAGE = new(5, 7);
     public Button B_R_FD_ON = new(4, 2);
     public Button B_BNK_SEL_10 = new(5, 6);
     public Button B_BNK_SEL_15 = new(5, 5);
     public Button B_BNK_SEL_20 = new(5, 4);
     public Button B_BNK_SEL_25 = new(5, 3);
     public Button B_BNK_SEL_30 = new(5, 2);
     private Button[] BUTTONS;

     public record Knob(int highByteOffset, int lowByteOffset)
     {
          public event Action<int>? onChange;
          private int? val;
          
          public void setVal(int newVal)
          {
               if (val != newVal)
               {
                    val = newVal;
                    onChange?.Invoke(newVal);
               }
          }
     }

     public Knob KNOB_CRS_L = new(22, 21);
     public Knob KNOB_IAS_MACH = new(24, 23);
     public Knob KNOB_HDG = new(26, 25);
     public Knob KNOB_ALT = new(28, 27);
     public Knob KNOB_VS = new(30, 29);
     public Knob KNOB_CRS_R = new(32, 31);
     private Knob[] KNOBS;
     
     private static readonly byte[] BLANK_DISPLAY_HID_REPORT =
     [
          0xf0, 0x0, 0x4, 0x38, 0xf, 0xbf, 0x0, 0x0, 0x2, 0x1, 0x0, 0x0, 0xf0, 0x03, 0xc,  0x0, 0x0, 0x2b, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
          // ================= Random Decimal points
          /*0*/         0x00, // IAS MACH Decimal point (LOL, why here?)
          /*1*/         0x00,
          /*2*/         0x00, 
          /*3*/         0x00,
          // ================ bit packed middle =============== 
          /*4*/         0x00, // 11101111 4th bit is not used
          /*5*/         0x00, // 11110111, 6th bit from the left is not used
          /*6*/         0x00, 
          /*7*/         0x00, 
          // ================ bit packed upper left ===============
          /*8*/         0x00, // CRSL + IAS_MACH V segment as binary with a gap between numbers
          /*9*/         0x00, // left most bit corresponds to middle of the 8 simbol
          /*10*/         0x00, // 4 dights of vspeed + 2 dights of alt
          /*11*/         0x00, // 17 CRS segments
          // ================ bit packed lower left  ===============
          /*12*/         0x00,
          /*13*/         0x00,
          /*14*/         0x00, 
          /*15*/         0x00, // Leftmost bit controls vspeed lower segment of plus, 4 rightmost bits unused
          // ================ bit packed bottom  ===============
          /*16*/         0x00,
          /*17*/         0x00,  // 4th bit from the right, controls TRK marker in HEADING. 7F max?
          /*18*/         0x00, 
          /*19*/         0x00, // Leftmost bit controls vspeed UPPER segment of plus, 4 rightmost bits unused
          // ================ bit packed lower right  ===============
          /*20*/         0x00, 
          /*21*/         0x00, // 4th bit from the right, controls TRK marker in HEADING, out of what (SAME AS ABOVE). Leftmost bit controls MACH in IAS/MACH
          /*22*/         0x00, 
          /*23*/         0x00, // 1F start, Leftmost bit controls FPA marker in VSpeed
          // ================ bit packed upper right  ===============
          /*24*/         0x00, 
          /*25*/         0x00,  // 4th bit from the right controls HDG marker in HEADING 
          /*26*/         0x00,
          /*27*/         0x00,
          // ================ bit packed top  ===============
          /*28*/         0x00, 
          /*29*/         0x00, // 1st bit from the left controls IAS marker in IAS, 5th bit from the left controls HDG marker in HEADING
          /*30*/         0x00, 
          /*31*/         0x00, // Leftmost bit controls TRK marker in VS
          0x0,0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xf0, 0x0, 0x5, 0x15, 0x0, 0x0, 0x0, 0x0, 0xf, 0xbf, 0x0, 0x0, 0x3,0x1, 0x0, 0x0, 0xf0, 0x03, 0xc, 
          0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 
          0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0
     ];

     private byte[] input;
     private HidStream stream;
     private HidDevice device;
     
     public static List<HidDevice> getDevices()
     {
          return DeviceList.Local.GetHidDevices(null, PAP3_PRODUCT_ID).ToList();
     }
     
     public PAP3N(HidDevice device)
     {
        this.device = device;
        ArgumentNullException.ThrowIfNull(device);
        if (device.ProductID.CompareTo(PAP3_PRODUCT_ID) != 0)
        {
             throw new ArgumentException("Device is not a PAP3");
        }

        CRS_R_DISPLAY = new(sendOutputReport, CRS_R_DIGIT_0, CRS_R_DIGIT_1, CRS_R_DIGIT_2);
        VS_DISPLAY = new(sendOutputReport, VS_DIGIT_0, VS_DIGIT_1, VS_DIGIT_2, VS_DIGIT_3, VS_PLUS_MINUS);
        ALT_DISPLAY = new(sendOutputReport, ALT_DIGIT_0, ALT_DIGIT_1, ALT_DIGIT_2, ALT_DIGIT_3, AlT_DIGIT_4);
        HDG_DISPLAY = new(sendOutputReport, HDG_DIGIT_0, HDG_DIGIT_1, HDG_DIGIT_2);
        IAS_DISPLAY = new(sendOutputReport, IAS_DIGIT_0, IAS_DIGIT_1, IAS_DIGIT_2, IAS_DIGIT_3);
        CRS_L_DISPLAY = new(sendOutputReport, CRS_L_DIGIT_0, CRS_L_DIGIT_1, CRS_L_DIGIT_2);
        BUTTONS =
        [
             B_N1, B_SPEED, B_VNAV, B_LVL_CHG, B_HDG_SEL, B_LNAV, B_VOR_LOC, B_APP, B_ALT_HOLD, B_VS, B_CMD_1,
             B_CWS_1,
             B_CMD_2, B_CSW_2, B_IAS_MACH_CO, B_SPD_INTV, B_ALT_INTV, B_L_FD_ON, B_AT_ARM_ON, B_AP_DISENGAGE, B_R_FD_ON,
             B_BNK_SEL_10, B_BNK_SEL_15, B_BNK_SEL_20, B_BNK_SEL_25, B_BNK_SEL_30
        ];

        KNOBS =
        [
             KNOB_CRS_L,
             KNOB_IAS_MACH,
             KNOB_HDG, KNOB_ALT, KNOB_VS, KNOB_CRS_R
        ];

        Action<byte[]> sendToDevice = v => stream?.WriteAsync(v, 0, v.Length);
        N1 = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        SPEED = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        VNAV = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        LVL_CHG = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x06, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        HDG_SEL = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        LNAV = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x08, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        VOR_LOC = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x09, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        APP = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0a, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        ALT_HOLD = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0b, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0b, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        VS = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0c, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        CMDA = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0d, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        CWSA = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0e, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        CMDB = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0f, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        CWSB = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        AT_ARM = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x11, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        MASTER_L = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x12, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        MASTER_R = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x13, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x13, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        SOLENOID = new Out(
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x1e, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00],
             [0x02, 0x0f, 0xbf, 0x00, 0x00, 0x03, 0x49, 0x1e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
             sendToDevice);
        LEDS =
        [
             N1, SPEED, APP, VS, ALT_HOLD, AT_ARM, CMDA, CMDB, CWSA, CWSB, HDG_SEL, LNAV, LVL_CHG, MASTER_L, MASTER_R, VNAV, VOR_LOC
        ];
     }

     public void connect()
     {
          device.TryOpen(out stream);
          input = new byte[device.GetMaxInputReportLength()];
          stream.BeginRead(input, 0, input.Length, processHIDReport, stream);
          Console.WriteLine("Connected to " + device.GetFriendlyName());
     }

     // Operations in the domain of Mode Control Panel: brightness setting, controlling Vals (LEDs, AT Solenoid), etc

     private void processHIDReport(IAsyncResult ar)
     {
          var stream = (HidStream)ar.AsyncState;
          try
          {
               if (stream!.EndRead(ar) > 0)
               {
                    onReportAvailable();
               }

               // Continue reading
               stream.BeginRead(input, 0, input.Length, processHIDReport, stream);
          }
          catch (Exception ex)
          {
               Console.WriteLine($"Error reading: {ex.Message}");
          }
     }

     private void onReportAvailable()
     {
          if (input[0] == 0x1)
          {
               foreach (var button in BUTTONS)
               {
                    var pressed = (input[button.byteOffset] >> (7 - button.bitOffset) & 1) > 0;
                    button.setVal(pressed);
               }

               foreach (var knob in KNOBS)
               {
                    var val = input[knob.lowByteOffset] | (input[knob.highByteOffset] << 8);
                    knob.setVal(val);
               }

               /*var inputReportDump = string.Join("|", input.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
               if (inputReportDump != lastReport)
               {
                    Console.WriteLine($"{inputReportDump.Substring(0, 250)}...");
                    lastReport = inputReportDump;
               }*/
          }
     }

     private void sendOutputReport()
     {
          // Start with blank displays
          var data = BLANK_DISPLAY_HID_REPORT;
          
          // Write out all output
          CRS_L_DISPLAY.writeString(data, 25);
          IAS_DISPLAY.writeString(data, 25);
          ALT_DISPLAY.writeString(data, 25);
          VS_DISPLAY.writeString(data, 25);
          CRS_R_DISPLAY.writeString(data, 25);
          
          // And spit it out
          stream.WriteAsync(data);
     }
     
     public void setPanelBrightness(byte brightness)
     {
          byte[] command = [0x2, 0xf, 0xbf, 0x0, 0x0, 0x3, 0x49, 0x0, brightness, 0x0, 0x0, 0x0, 0x0, 0x0 ];
          stream.WriteAsync(command);
     }
     
     public void setDigitBrightness(byte brightness)
     {
          byte[] command = [0x2, 0xf, 0xbf, 0x0, 0x0, 0x3, 0x49, 0x1, brightness, 0x0, 0x0, 0x0, 0x0, 0x0];
          stream.WriteAsync(command);
     }
     
     public void setButtonBrightness(byte brightness)
     {
          byte[] command = [0x2, 0xf, 0xbf, 0x0, 0x0, 0x3, 0x49, 0x2, brightness, 0x0, 0x0, 0x0, 0x0, 0x0];
          stream.WriteAsync(command);
     }
}