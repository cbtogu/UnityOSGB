﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ObjectBase
{
    public static string ReadString(BinaryReader reader)
    {
        int strLength = reader.ReadInt32();
        char[] compressor = reader.ReadChars(strLength);
        return new string(compressor);
    }

    public static long ReadBracket(BinaryReader reader, ReaderOSGB owner)
    {
        if (owner._useBrackets)
        {
            if (owner._version > 148) return reader.ReadInt64();
            else return reader.ReadInt32();
        }
        return -1;
    }

    public static Texture2D LoadImage(Object gameObj, BinaryReader reader, ReaderOSGB owner)
    {
        Texture2D tex2D = null;
        if (owner._version > 94) { string className = ReadString(reader); }

        uint id = reader.ReadUInt32();
        if (owner._sharedTextures.ContainsKey(id))
            return owner._sharedTextures[id];

        string fileName = ReadString(reader);
        int writeHint = reader.ReadInt32();
        int decision = reader.ReadInt32();
        switch (decision)
        {
            case 0:  // IMAGE_INLINE_DATA
                {
                    int origin = reader.ReadInt32();
                    int s = reader.ReadInt32();
                    int t = reader.ReadInt32();
                    int r = reader.ReadInt32();
                    int internalFormat = reader.ReadInt32();
                    int pixelFormat = reader.ReadInt32();
                    int dataType = reader.ReadInt32();
                    int packing = reader.ReadInt32();
                    int mode = reader.ReadInt32();

                    uint size = reader.ReadUInt32();
                    if (size > 0)
                    {
                        byte[] imageData = reader.ReadBytes((int)size);

                        TextureFormat format = TextureFormat.RGB24;  // TODO: other formats/data size
                        if (pixelFormat == 0x1908) format = TextureFormat.RGBA32;  // GL_RGBA
                        tex2D = new Texture2D(s, r, format, false);
                        tex2D.LoadRawTextureData(imageData);
                        tex2D.Apply();
                    }

                    uint numLevels = reader.ReadUInt32();
                    for (uint i = 0; i < numLevels; ++i)
                    {
                        uint levelDataSize = reader.ReadUInt32();
                        // TODO
                    }
                }
                break;
            case 1:  // IMAGE_INLINE_FILE
                {
                    uint size = reader.ReadUInt32();
                    if (size > 0)
                    {
                        byte[] fileData = reader.ReadBytes((int)size);

                        tex2D = new Texture2D(2, 2);
                        tex2D.LoadImage(fileData);
                        //File.WriteAllBytes("test.jpg", fileData);
                    }
                }
                break;
            case 2:  // IMAGE_EXTERNAL
                if (File.Exists(fileName))
                {
                    byte[] fileData = File.ReadAllBytes(fileName);

                    tex2D = new Texture2D(2, 2);
                    tex2D.LoadImage(fileData);
                }
                break;
            default: break;
        }

        osg_Object classObj = new osg_Object();
        classObj.read(gameObj, reader, owner);
        owner._sharedTextures[id] = tex2D;
        return tex2D;
    }

    public static bool LoadObject(Object gameObj, BinaryReader reader, ReaderOSGB owner)
    {
        string className = ReadString(reader);
        long blockSize = ReadBracket(reader, owner);
        uint id = reader.ReadUInt32();
        className = className.Replace("::", "_");

        if (owner._sharedObjects.ContainsKey(id))
        {
            //Debug.Log("Shared object " + className + "-" + id);
            return true;  // TODO: how to share nodes?
        }
        else
            owner._sharedObjects[id] = gameObj;

        System.Type classType = System.Type.GetType(className);
        if (classType == null)
        {
            Debug.LogWarning("Object type " + className + " not implemented");
            return false;
        }

        ObjectBase classObj = System.Activator.CreateInstance(classType) as ObjectBase;
        if (classObj == null)
        {
            Debug.LogWarning("Object instance " + className + " failed to create");
            return false;
        }
        else
            return classObj.read(gameObj, reader, owner);
    }

    public virtual bool read(Object gameObj, BinaryReader reader, ReaderOSGB owner)
    { Debug.LogWarning("Method read() not implemented"); return false; }
}
