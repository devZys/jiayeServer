using System;
using System.IO;
using System.Text;
using zlib;
using System.Security.Cryptography;

namespace LegendProtocol
{    
    //MD5加密
    public class MyMD5
    {
        //加密为32字符长度的16进制字符串
        public static string Encrypt(string input)
        {
            try
            {
                MD5 md5Hasher = MD5.Create();
                byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }
        //加密为16字符长度的16进制字符串
        public static string EncryptToStr16(string input)
        {
            try
            {
                using (MD5 md5Hasher = MD5.Create())
                {
                    string t2 = BitConverter.ToString(md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input)), 4, 8);
                    return t2.Replace("-", "").ToLower();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
    }

    //Base64加解密
    public class MyBase64
    {
        //加密
        public static string Encrypt(string input)
        {
            try
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
            }
            catch(Exception)
            {
                return input;
            }
        }
        //解密
        public static string Decrypt(string input)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(input));
            }
            catch (Exception)
            {
                return input;
            }
        }
    }

    //自定义GUID
    public class MyGuid
    {
        /**
          本程序可以生成64位全游戏大世界全局唯一ID，适合在分布式服务器集群中产生唯一ID，也适合分区分服的服务器，可自由更改掩码与占位调节
        　特点比微软自带的GUID要节省一倍的空间，即只需要64位的int即可，因此基本上在游戏服务器领域可以抛弃微软的那个GUID了
          支持每秒生成65536个,在此条件基础上这辈子都不会产生相同的ID

          由 时间戳+服务类型号+服务器ID号+本地递增序号 组成

          时间戳 32bit
          服务类型号 4bit[允许16种不同类型的服务]
          服务器编号 12bit [同一类型服务器允许4096台]
          递增序号 16bit [该位数决定了每秒允许65536个不同的guid]
        */
        private static ulong c_mark_time_stamp = 0xffffffff00000000;/*时间戳掩码*/
        private static ulong c_mark_service_type = 0x00000000f0000000;/*服务类型掩码*/
        private static ulong c_mark_server_id = 0x000000000fff0000;/*服务器编号掩码*/
        private static ulong c_mark_base = 0x000000000000ffff;/*本地ID编号掩码*/
        private static uint c_baseId = 0;/*本地ID*/
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //生成唯一ID
        public static ulong NewGuid(ServiceType service, uint id)
        {
            uint serviceType = (uint)service;
            ulong timeStamp = (ulong)(DateTime.UtcNow - Jan1st1970).TotalSeconds;
            ulong newId = ((timeStamp << 32) & c_mark_time_stamp) | ((serviceType << 28) & c_mark_service_type) | ((id << 16) & c_mark_server_id) | (c_baseId & c_mark_base);

            c_baseId++;

            return newId;
        }
        //根据唯一ID获取所在服务类型与服务器编号
        public static void GetService(ulong guid, out ServiceType type, out uint id)
        {
            type = (ServiceType)((guid & c_mark_service_type) >> 28);
            id = (uint)((guid & c_mark_server_id) >> 16);
        }
    }

    //序列化器
    public class Serializer
    {
        private static readonly MsgSerializer msgSerializer = new MsgSerializer();
        public static byte[] tryCompressMsg(object msg, out int originalSize)
        {
            if (msg == null)
            {
                originalSize = 0;
                return null;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    msgSerializer.Serialize(ms, msg);

                    byte[] msgByte = ms.ToArray();
                    if (msgByte.Length > int.MaxValue)
                    {
                        originalSize = 0;
                        return null;
                    }
                    originalSize = msgByte.Length;
                    if (originalSize < Util.needCompressSize)
                    {
                        return msgByte;
                    }
                    return compress(msgByte);
                }
            }
            catch (Exception)
            {
                originalSize = 0;
                return null;
            }
        }
        public static object tryUncompressMsg(byte[] msgByte, bool unCompress, Type type)
        {
            if (msgByte == null) return null;

            try
            {
                if (unCompress)
                {
                    byte[] newMsgByte = uncompress(msgByte);
                    if (newMsgByte == null) return null;

                    using (MemoryStream ms = new MemoryStream(newMsgByte, 0, newMsgByte.Length))
                    {
                        return msgSerializer.Deserialize(ms, null, type);
                    }
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(msgByte, 0, msgByte.Length))
                    {
                        return msgSerializer.Deserialize(ms, null, type);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static byte[] trySerializerObject(object obj)
        {
            if (obj == null) return null;

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    msgSerializer.Serialize(ms, obj);

                    byte[] msgByte = ms.ToArray();
                    if (msgByte.Length >= int.MaxValue)
                    {
                        return null;
                    }
                    return msgByte;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static T tryUnSerializerObject<T>(byte[] msgByte)
        {
            if (msgByte == null) return default(T);
            try
            {
                using (MemoryStream ms = new MemoryStream(msgByte, 0, msgByte.Length))
                {
                    return (T)msgSerializer.Deserialize(ms, null, typeof(T));
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }
        public static byte[] tryCompressObject(object obj)
        {
            if (obj == null) return null;

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    msgSerializer.Serialize(ms, obj);
                    return compress(ms.ToArray());
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static T tryUncompressObject<T>(byte[] msgByte)
        {
            if (msgByte == null) return default(T);
            try
            {
                byte[] newMsgByte = uncompress(msgByte);
                if (newMsgByte == null) return default(T);

                using (MemoryStream ms = new MemoryStream(newMsgByte, 0, newMsgByte.Length))
                {
                    return (T)msgSerializer.Deserialize(ms, null, typeof(T));
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }
        public static byte[] compress(byte[] byteData)
        {
            if (byteData == null) return null;
            if (byteData.Length == 0) return byteData;

            MemoryStream outStream = new MemoryStream();
            ZOutputStream outZStream = new ZOutputStream(outStream, zlibConst.Z_DEFAULT_COMPRESSION);

            try
            {
                outZStream.Write(byteData, 0, byteData.Length);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                outZStream.finish();
                outZStream.Close();
                outStream.Flush();
                outStream.Close();
            }
            return outStream.ToArray();
        }
        public static byte[] uncompress(byte[] byteData)
        {
            if (byteData == null) return null;
            if(byteData.Length == 0) return byteData;

            MemoryStream outStream = new MemoryStream();
            ZOutputStream outZStream = new ZOutputStream(outStream);
            MemoryStream inStream = null;
            try
            {
                inStream = new MemoryStream(byteData);
                CopyStream(inStream, outZStream);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                outZStream.Close();
                outStream.Close();
                if (inStream != null)
                {
                    inStream.Close();
                }
            }

            return outStream.ToArray();
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
