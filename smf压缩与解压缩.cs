using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //c# zlib的测试  及中文版安装包内smf文件压缩与解压
            //这次由我SmallPea向各位展示smf数据包的解压与压缩（安装包内smf与Android/data下的smf 的互转）

            Console.WriteLine("开始进行处理...");
            Stopwatch time = new Stopwatch();
            time.Start();
            
            //自己写调用，运行
            FileStream input = new FileStream(@"E:\zlib\dev1.rsb.smf", FileMode.Open);
            SmfZlibUnCompressed(input, @"E:\zlib\dev1.rsb.smf.jj");
            
            
            time.Stop();
            Console.WriteLine("操作处理结束，耗时{0}毫秒",time.ElapsedMilliseconds.ToString());
            Console.ReadLine();
        }
        //c#实现中文版的zlib压缩与解压缩
        private static  void SmfZlibCompressed(Stream  inputStream,CompressionLevel level, string outPutpath)
        {
            //输入流不关闭
            byte[] smfHeader = { 0xD4, 0xFE, 0xAD, 0xDE };//前4个字节固定，后4个为解压后文件大小
            smfHeader = bytesArrayMerged(smfHeader,getFalseBytes(inputStream.Length,4));
            File.WriteAllBytes(outPutpath,bytesArrayMerged(smfHeader,ZlibCompressed(inputStream,level)));
              
        }
        private  static void SmfZlibUnCompressed(Stream inputStream, string outPutpath)
        {
            //跳过前8个字节即可，然后zlib解压
            //输入流不关闭
            inputStream.Seek(8,SeekOrigin.Begin);
            byte[] zlibPartBytesBuff = new byte[1024];
            int trueReadlen = 0;
            MemoryStream zlibPart = new MemoryStream();
            while ((trueReadlen=inputStream.Read(zlibPartBytesBuff))!=0)
            {
                zlibPart.Write(zlibPartBytesBuff,0,trueReadlen);
            }
            File.WriteAllBytes(outPutpath,ZlibUnCompressed(zlibPart));
            
        }

        private static  byte[] bytesArrayMerged(byte[] ahead, byte[] hinder)
        {
            byte[] merged = new byte[ahead.Length + hinder.Length];
            for (int i = 0; i < ahead.Length; i++)
            {
                merged[i] = ahead[i];
            }
            for (int i = 0; i < hinder.Length; i++)
            {
                merged[ahead.Length + i] = hinder[i];
            }
            
            return merged;
        }
        private static  byte[] getAdler32Bytes(Stream input)
        {
            //输入流勿关闭
            byte[] inputbytes = new byte[input.Length];
            input.Read(inputbytes);
            input.Seek(0, SeekOrigin.Begin);//流传入后由于该方法调用过读，因此下面需要将指针归0
            uint adler32Value = Adler32Hash(inputbytes, 0, inputbytes.Length);
            String ValueHex = Convert.ToString(adler32Value,16);
            if (ValueHex.Length > 8)
            {
                throw new Exception("Adler32长度异常");
            }
            ValueHex = stringMakeup(ValueHex, 4*2);
            byte[] result = new byte[4];
          
            for (int i = 0; i < 4; i++)
            {
              
                result[i] = Convert.ToByte(ValueHex.Substring(i*2,2),16);
            }
            return result;
        }
        public static uint Adler32Hash(byte[] bytesArray, int byteStart, int bytesToRead)
        {//Adler32算法源码
            int n;
            uint checksum = 1;
            uint s1 = checksum & 0xFFFF;
            uint s2 = checksum >> 16;

            while (bytesToRead > 0)
            {
                n = (3800 > bytesToRead) ? bytesToRead : 3800;
                bytesToRead -= n;
                while (--n >= 0)
                {
                    s1 = s1 + (uint)(bytesArray[byteStart++] & 0xFF);
                    s2 = s2 + s1;
                }
                s1 %= 65521;
                s2 %= 65521;
            }
            checksum = (s2 << 16) | s1;
            return checksum;
        }

        private static  byte[] ZlibCompressed(Stream  inputStream, CompressionLevel level)
        {
            //输入流请勿关闭
            //java中默认压缩级别-1其实为6
            byte[] header = {0x78,0};
            if (level == CompressionLevel.Fastest)
            {//快速压缩 没细究级别到底多少 按6
                header[1] = 0x9C;
            } else if (level == CompressionLevel.Optimal)
            {//最优压缩 级别9
                header[1] = 0xDA;
            }else
            {//不压缩 ,并在前面加上一些5字节的信息
                header[1] = 0x01;
            }
            byte[] ender = getAdler32Bytes(inputStream);
           MemoryStream outStream = new MemoryStream();
            DeflateStream compresser = new DeflateStream(outStream ,level,true);
            int trueReadbytes = -1;
            byte[] buff = new byte[1024];
            while ((trueReadbytes = inputStream.Read(buff) )!= 0){
                compresser.Write(buff,0,trueReadbytes);
            }
            compresser.Close();
              return bytesArrayMerged(header,bytesArrayMerged(outStream.ToArray(),ender));
        }
        private static  byte[] ZlibUnCompressed(Stream  inputStream)
        {
            byte[] deflateData = new byte[inputStream.Length-6];
            inputStream.Seek(2,SeekOrigin.Begin);
            inputStream.Read(deflateData);
            MemoryStream inStream = new MemoryStream(deflateData);
            MemoryStream outStream = new MemoryStream();
            DeflateStream unCompresser = new DeflateStream(inStream, CompressionMode.Decompress,true);
            int trueReadbytes = -1;
            byte[] buff = new byte[1024];
            while ((trueReadbytes = unCompresser.Read(buff)) != 0)
            {
                outStream.Write(buff, 0, trueReadbytes);
            }

            return outStream.ToArray();
        }

        private  static string stringMakeup(string src,int length)
        {//用于补齐字符串，用0往前补充
            if (src.Length > length)
            {
                throw new Exception("设定的长度低于原字符串长度");
            }
            int needMakeupZeroSum = length - src.Length;
            StringBuilder re = new StringBuilder(src);
            for (int i = 0; i < needMakeupZeroSum; i++)
            {
                re.Insert(0, "0");
            }
            return re.ToString();
        }





        private static  long getTrueLength(byte []src)
        {//获取翻转的字节真实值
            StringBuilder HexLen = new StringBuilder();
            foreach ( byte onebyte in src)
            {
                HexLen.Insert(0,Convert.ToString(onebyte,16));
            }
            return Convert.ToInt64(HexLen);
        }
        
        private static  byte[] getFalseBytes(long src ,int byteslength) 
        {
            byte[] srcfalsebytes = getFalseBytes(src);
            byte[] falsebytes = new byte[byteslength];
            if (srcfalsebytes.Length>byteslength)
            {
                throw new Exception("设定字节长度小于真实长度");
            }
            else
            {
                for (int i = 0; i < srcfalsebytes.Length; i++)
                {
                    falsebytes[i] = srcfalsebytes[i];
                }
            }
            return falsebytes;
        }
        private static  byte[] getFalseBytes(long src)
        {//获取long16进制字符串分割后翻转字节数组
            //任意长度字节长度
            string lengthhex = Convert.ToString(src, 16);
         
            if (lengthhex.Length / 2 != (lengthhex.Length / 2 * 2))
            {
                lengthhex = "0" + lengthhex;
            }
           
            byte[] refalsebytes = new byte[lengthhex .Length/2];
            for (int i = refalsebytes.Length; i>0; i--)
            {
                refalsebytes[i-1] = Convert.ToByte(lengthhex.Substring(lengthhex.Length-i*2, 2));
            }
           
            return refalsebytes;
        }


    }
 
}
