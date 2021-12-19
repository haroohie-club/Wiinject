using System.Collections.Generic;
using System.Linq;

namespace Wiinject.Tests
{
    public static class TestHelpers
    {
        public static string ToHexString(this IEnumerable<byte> byteEnumerable)
        {
            return string.Join(' ', byteEnumerable.Select(b => $"{b:X2}"));
        }

        public const string TestFunctionCallAsm = @"stwu 1,-24(1)
mflr 0
stw 0,20(1)
stw 31,16(1)
mr 31,1
mr 3,26
bl =test_function
lwz 0,20(1)
mtlr 0
addi 11,31,24
lwz 31,-4(11)
mr 1,11
blr";

        public const string TestFunctionC = @":
 18001e0:       94 21 ff e8     stwu    r1,-24(r1)
 18001e4:       93 e1 00 14     stw     r31,20(r1)
 18001e8:       7c 3f 0b 78     mr      r31,r1
 18001ec:       7c 69 1b 78     mr      r9,r3
 18001f0:       99 3f 00 08     stb     r9,8(r31)
 18001f4:       89 3f 00 08     lbz     r9,8(r31)
 18001f8:       2c 09 00 6c     cmpwi   r9,108
 18001fc:       41 82 00 44     beq     1800240 <test_function+0x60>
 1800200:       2c 09 00 6c     cmpwi   r9,108
 1800204:       41 81 00 44     bgt     1800248 <test_function+0x68>
 1800208:       2c 09 00 69     cmpwi   r9,105
 180020c:       41 82 00 34     beq     1800240 <test_function+0x60>
 1800210:       2c 09 00 69     cmpwi   r9,105
 1800214:       41 81 00 34     bgt     1800248 <test_function+0x68>
 1800218:       2c 09 00 49     cmpwi   r9,73
 180021c:       41 82 00 24     beq     1800240 <test_function+0x60>
 1800220:       2c 09 00 49     cmpwi   r9,73
 1800224:       41 81 00 24     bgt     1800248 <test_function+0x68>
 1800228:       2c 09 00 21     cmpwi   r9,33
 180022c:       41 82 00 14     beq     1800240 <test_function+0x60>
 1800230:       2c 09 00 41     cmpwi   r9,65
 1800234:       40 82 00 14     bne     1800248 <test_function+0x68>
 1800238:       39 20 01 80     li      r9,384
 180023c:       48 00 00 10     b       180024c <test_function+0x6c>
 1800240:       39 20 00 48     li      r9,72
 1800244:       48 00 00 08     b       180024c <test_function+0x6c>
 1800248:       39 20 00 90     li      r9,144
 180024c:       7d 23 4b 78     mr      r3,r9
 1800250:       39 7f 00 18     addi    r11,r31,24
 1800254:       83 eb ff fc     lwz     r31,-4(r11)
 1800258:       7d 61 5b 78     mr      r1,r11
 180025c:       4e 80 00 20     blr
 1800260:       80 01 00 0c     lwz     r0,12(r1)
 1800264:       38 21 00 08     addi    r1,r1,8
 1800268:       7c 08 03 a6     mtlr    r0
 180026c:       4e 80 00 20     blr

01800270";

        public const string RecursionTestC = @":
 18001e0:       94 21 ff e8     stwu    r1,-24(r1)
 18001e4:       7c 08 02 a6     mflr    r0
 18001e8:       90 01 00 1c     stw     r0,28(r1)
 18001ec:       93 e1 00 14     stw     r31,20(r1)
 18001f0:       7c 3f 0b 78     mr      r31,r1
 18001f4:       90 7f 00 08     stw     r3,8(r31)
 18001f8:       81 3f 00 08     lwz     r9,8(r31)
 18001fc:       2c 09 00 00     cmpwi   r9,0
 1800200:       40 81 00 1c     ble     180021c <recursion_test+0x3c>
 1800204:       81 3f 00 08     lwz     r9,8(r31)
 1800208:       39 29 ff ff     addi    r9,r9,-1
 180020c:       7d 23 4b 78     mr      r3,r9
 1800210:       4b ff ff d1     bl      18001e0 <recursion_test>
 1800214:       7c 69 1b 78     mr      r9,r3
 1800218:       48 00 00 08     b       1800220 <recursion_test+0x40>
 180021c:       39 20 00 0a     li      r9,10
 1800220:       7d 23 4b 78     mr      r3,r9
 1800224:       39 7f 00 18     addi    r11,r31,24
 1800228:       80 0b 00 04     lwz     r0,4(r11)
 180022c:       7c 08 03 a6     mtlr    r0
 1800230:       83 eb ff fc     lwz     r31,-4(r11)
 1800234:       7d 61 5b 78     mr      r1,r11
 1800238:       4e 80 00 20     blr
 180023c:       80 01 00 0c     lwz     r0,12(r1)
 1800240:       38 21 00 08     addi    r1,r1,8
 1800244:       7c 08 03 a6     mtlr    r0
 1800248:       4e 80 00 20     blr

0180024c";

        public const string ReturnTestC = @":
 18001e0:       94 21 ff e8     stwu    r1,-24(r1)
 18001e4:       93 e1 00 14     stw     r31,20(r1)
 18001e8:       7c 3f 0b 78     mr      r31,r1
 18001ec:       90 7f 00 08     stw     r3,8(r31)
 18001f0:       39 20 00 05     li      r9,5
 18001f4:       7d 23 4b 78     mr      r3,r9
 18001f8:       39 7f 00 18     addi    r11,r31,24
 18001fc:       83 eb ff fc     lwz     r31,-4(r11)
 1800200:       7d 61 5b 78     mr      r1,r11
 1800204:       4e 80 00 20     blr

01800208";

        public const string CallTestC = @":
 1800208:       94 21 ff e8     stwu    r1,-24(r1)
 180020c:       7c 08 02 a6     mflr    r0
 1800210:       90 01 00 1c     stw     r0,28(r1)
 1800214:       93 e1 00 14     stw     r31,20(r1)
 1800218:       7c 3f 0b 78     mr      r31,r1
 180021c:       90 7f 00 08     stw     r3,8(r31)
 1800220:       80 7f 00 08     lwz     r3,8(r31)
 1800224:       4b ff ff bd     bl      18001e0 <return_test>
 1800228:       7c 69 1b 78     mr      r9,r3
 180022c:       7d 23 4b 78     mr      r3,r9
 1800230:       39 7f 00 18     addi    r11,r31,24
 1800234:       80 0b 00 04     lwz     r0,4(r11)
 1800238:       7c 08 03 a6     mtlr    r0
 180023c:       83 eb ff fc     lwz     r31,-4(r11)
 1800240:       7d 61 5b 78     mr      r1,r11
 1800244:       4e 80 00 20     blr
 1800248:       80 01 00 0c     lwz     r0,12(r1)
 180024c:       38 21 00 08     addi    r1,r1,8
 1800250:       7c 08 03 a6     mtlr    r0
 1800254:       4e 80 00 20     blr

01800258";
    }
}
