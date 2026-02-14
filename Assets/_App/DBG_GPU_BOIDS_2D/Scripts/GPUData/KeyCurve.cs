using UnityEngine;
using System;

[Serializable]
public struct GPUSprite
{
    public int textureI;
    public Vector2 uv;
    public Vector2 sizeuv;

    public int GetSize()
    {
        return sizeof(int) * 1 + sizeof(float) * 4;
    }
};

[Serializable]
public struct Entity
{
    public int nodesStartI;     //No se necesita en los shaders
    public int nodesC;
    public int clipsStartI;

    public int GetSize()
    {
        return sizeof(int) * 3;
    }
};

[Serializable]
public struct Node          //INNECESARIO
{
    public int entityI;
    public int parentI;     //INNECESARIO
    public int order;

    public int GetSize()
    {
        return sizeof(int) * 3;
    }
};

[Serializable]
public struct Clip
{
    public int frameStartI;
    public int framesC;
    public float frameDuration;

    public int GetSize()
    {
        return sizeof(int) * 2 + sizeof(float) * 1;
    }
};

[Serializable]
public struct Frame
{
    public Matrix3x3 transformOS;
    public int spriteI;

    public int GetSize()
    {
        return sizeof(int) * 1 + sizeof(float) * 9;
    }
};

[Serializable]
public struct EntityInstance
{
    public int entityI;
    public int currentClipI;
    public Vector4 transformWS;
    public float time;

    public int GetSize()
    {
        return sizeof(int) * 2 + sizeof(float) * 5;
    }

    public override string ToString()
    {
        return "entityI = "+ entityI
            + "\ncurrentClipI = "+ currentClipI;
    }
}

[Serializable]
public struct NodeInstance
{
    public int entityInstanceI;
    public int order;

    public int GetSize()
    {
        return sizeof(int) * 2;
    }

    public override string ToString()
    {
        return "entityInstanceI = "+ entityInstanceI;
    }
}

struct NodeInstanceFrame
{
    public int spriteI;
    public Matrix3x3 transformOS;
    public float flip;

    public int GetSize()
    {
        return sizeof(int) * 1 + sizeof(float) * 10;
    }
};

[Serializable]
public struct Matrix3x3
{   
    public float _00, _01, _02, _10, _11, _12, _20, _21, _22;
    public static Matrix3x3 I = new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

    public Matrix3x3(float __00, float __01, float __02, float __10, float __11, float __12, float __20, float __21, float __22)
    {
        _00 = __00;
        _01 = __01;
        _02 = __02;
        _10 = __10;
        _11 = __11;
        _12 = __12;
        _20 = __20;
        _21 = __21;
        _22 = __22;
    }

    public void SetInputs(float __00, float __01, float __02, float __10, float __11, float __12, float __20, float __21, float __22)
    {
        _00 = __00;
        _01 = __01;
        _02 = __02;
        _10 = __10;
        _11 = __11;
        _12 = __12;
        _20 = __20;
        _21 = __21;
        _22 = __22;
    }

    public Matrix3x3(Vector3 row0, Vector3 row1, Vector3 row2)
    {
        _00 = row0.x; _01 = row0.y; _02 = row0.z;
        _10 = row1.x; _11 = row1.y; _12 = row1.z;
        _20 = row2.x; _21 = row2.y; _22 = row2.z;
    }

    public Matrix3x3(Vector2 translation, Vector2 scale, float rotation)
    {
        _00 = scale.x * Mathf.Cos(rotation); _01 = -Mathf.Sin(rotation); _02 = translation.x;
        _10 = Mathf.Sin(rotation); _11 = scale.y * Mathf.Cos(rotation); _12 = translation.y;
        _20 = 0; _21 = 0; _22 = 1;
    }

    public Vector2 TranslationVector
    {
        get { return new Vector2(_02, _12); }
    }

    public Vector2 ScateVector
    {
        get { return new Vector2(_00, _11); }
    }

    public override string ToString()
    {
        return
            _00 + ", " + _01 + ", " + _02 + "\n" +
            _10 + ", " + _11 + ", " + _12 + "\n" +
            _20 + ", " + _21 + ", " + _22;
    }

    public static Matrix3x3 operator +(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a._00 + b._00, a._01 + b._01, a._02 + b._02,
            a._10 + b._10, a._11 + b._11, a._12 + b._12,
            a._20 + b._20, a._21 + b._21, a._22 + b._22
            );
    }

    public static Matrix3x3 operator -(Matrix3x3 a)
    {
        return new Matrix3x3(
            -a._00, -a._01, -a._02,
            -a._10, -a._11, -a._12,
            -a._20, -a._21, -a._22
            );
    }

    public static Matrix3x3 operator -(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a._00 - b._00, a._01 - b._01, a._02 - b._02,
            a._10 - b._10, a._11 - b._11, a._12 - b._12,
            a._20 - b._20, a._21 - b._21, a._22 - b._22
            );
    }

    public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a._00 * b._00 + a._01 * b._10 + a._02 * b._20,
            a._00 * b._01 + a._01 * b._11 + a._02 * b._21,
            a._00 * b._02 + a._01 * b._12 + a._02 * b._22,

            a._10 * b._00 + a._11 * b._10 + a._12 * b._20,
            a._10 * b._01 + a._11 * b._11 + a._12 * b._21,
            a._10 * b._02 + a._11 * b._12 + a._12 * b._22,

            a._20 * b._00 + a._21 * b._10 + a._22 * b._20,
            a._20 * b._01 + a._21 * b._11 + a._22 * b._21,
            a._20 * b._02 + a._21 * b._12 + a._22 * b._22

            );
    }

    public static Vector3 operator *(Matrix3x3 a, Vector3 b)
    {
        return new Vector3(
            a._00 * b.x + a._01 * b.y + a._02 * b.z,
            a._10 * b.x + a._11 * b.y + a._12 * b.z,
            a._20 * b.x + a._21 * b.y + a._22 * b.z
        );
    }

    public static Matrix3x3 Translation(Vector3 v)
    {
        return new Matrix3x3(1, 0, v.x, 0, 1, v.y, 0, 0, v.z);
    }

    public static Matrix3x3 Translation(Vector2 v)
    {
        return new Matrix3x3(1, 0, v.x, 0, 1, v.y, 0, 0, 1);
    }

    public static Matrix3x3 Scale(Vector2 v)
    {
        return new Matrix3x3(v.x, 0, 0, 0, v.y, 0, 0, 0, 1);
    }

    public int GetSize()
    {
        return sizeof(float) * 9;
    }
}

public struct Matrix2x2
{
    public static Matrix2x2 I = new Matrix2x2(1, 0, 0, 1);
    public float _00, _01, _10, _11;

    public Matrix2x2(float __00, float __01, float __10, float __11)
    {
        _00 = __00;
        _01 = __01;
        _10 = __10;
        _11 = __11;
    }

    public void SetInputs(float __00, float __01, float __10, float __11)
    {
        _00 = __00;
        _01 = __01;
        _10 = __10;
        _11 = __11;
    }

    public Matrix2x2(Vector2 row0, Vector2 row1)
    {
        _00 = row0.x; _01 = row0.y;
        _10 = row1.x; _11 = row1.y;
    }

    public override string ToString()
    {
        return
            _00 + ", " + _01 + "\n" +
            _10 + ", " + _11 + "\n";
    }

    public static Matrix2x2 operator +(Matrix2x2 a, Matrix2x2 b)
    {
        return new Matrix2x2(
            a._00 + b._00, a._01 + b._01,
            a._10 + b._10, a._11 + b._11
            );
    }

    public static Matrix2x2 operator -(Matrix2x2 a)
    {
        return new Matrix2x2(
            -a._00, -a._01, 
            -a._10, -a._11
            );
    }

    public static Matrix2x2 operator -(Matrix2x2 a, Matrix2x2 b)
    {
        return new Matrix2x2(
            a._00 - b._00, a._01 - b._01,
            a._10 - b._10, a._11 - b._11
            );
    }

    public static Matrix2x2 operator *(Matrix2x2 a, Matrix2x2 b)
    {
        return new Matrix2x2(
            a._00 * b._00 + a._01 * b._10,
            a._00 * b._01 + a._01 * b._11,

            a._10 * b._00 + a._11 * b._10,
            a._10 * b._01 + a._11 * b._11
            );
    }

    public static Vector2 operator *(Matrix2x2 a, Vector2 b)
    {
        return new Vector2(
            a._00 * b.x + a._01 * b.y,
            a._10 * b.x + a._11 * b.y
        );
    }

    public int GetSize()
    {
        return sizeof(float) * 4;
    }
}

public class SpriteFrame
{
    public int spriteI;
    public Vector2 translation;
    public float rotation;
    public Vector2 scale = Vector2.one;
    public Vector2 pivot = Vector2.zero;
    Sprite m_sprite;

    public SpriteFrame(Sprite sprite, Transform t)
    {
        translation = new Vector2(t.localPosition.x, t.localPosition.y);
        rotation = Mathf.Deg2Rad * t.eulerAngles.z;
        scale = new Vector2(t.localScale.x, t.localScale.y);
        m_sprite = sprite;
    }

    public Sprite Sprite {
        get { return m_sprite; }
        set {
            m_sprite = value;
            pivot = (m_sprite.pivot - m_sprite.rect.min) / m_sprite.pixelsPerUnit;
        }
    }

    public void Set(string property, float value)
    {
        string prop = property.ToLower();

        switch(property)
        {
            case "m_LocalPosition.x":
                translation.x = value;
                break;
            case "m_LocalPosition.y":
                translation.y = value;
                break;
            case "m_LocalScale.x":
                scale.x = value;
                break;
            case "m_LocalScale.y":
                scale.y = value;
                break;
            case "localEulerAnglesRaw.z":
                rotation = Mathf.Deg2Rad * value;
                break;
        }

        /*
        if (prop.Contains("position"))
        {
            if (prop.Contains('x'))
                translation.x = value;
            else if (prop.Contains('y'))
                translation.y = value;
        }
        else if (prop.Contains("rotation"))
        {
            if (prop.Contains('z'))
                rotation = Mathf.Deg2Rad * value;
        }
        else if(prop.Contains("scale"))
        {
            if (prop.Contains('x'))
                scale.x = value;
            else if (prop.Contains('y'))
                scale.y = value;
        }
        */
    }

    public Vector2 SpritePosition
    {
        get
        {
            Vector2[] vertices = m_sprite.vertices;
            return vertices[2];
        }
    }

    public Vector2 SpriteSize {
        get {
            Vector2[] vertices = m_sprite.vertices;
            return vertices[1] - vertices[2];
        }
    }

    public Matrix3x3 SpriteScale
    {
        get
        {
            Vector2[] vertices = m_sprite.vertices;
            Vector2 size = vertices[1] - vertices[2];
            return Matrix3x3.Scale(size);
        }
    }

    public Vector2[] Vectices
    {
        get
        {
            return m_sprite.vertices;
        }
    }

    public Matrix3x3 SpriteTranslation
    {
        get
        {
            return Matrix3x3.Translation(m_sprite.vertices[2]);
        }
    }

    public Matrix3x3 SpriteTransform
    {
        get
        {
            return SpriteTranslation * SpriteScale;
        }
    }

    public Matrix3x3 Translation
    {
        get
        {
            return new Matrix3x3(
                1, 0, translation.x,
                0, 1, translation.y,
                0, 0, 1
            );
        }
    }

    public Matrix3x3 FullTransform {
        get
        {
            //Matrix3x3 piv = Matrix3x3.Translation(pivot);
            Matrix3x3 spTra = SpriteTranslation;
            Matrix3x3 spSca = SpriteScale;
            Matrix3x3 rot = Rotation;
            Matrix3x3 sca = Scale;
            Matrix3x3 tra = Translation;
            //return (piv * ((rot * sca) * -piv)) * tra;

            return tra * rot * sca * spTra * spSca;
        }
    }

    public Matrix3x3 NodeTransform
    {
        get
        {
            //Matrix3x3 piv = Matrix3x3.Translation(pivot);
            Matrix3x3 rot = Rotation;
            Matrix3x3 sca = Scale;
            Matrix3x3 tra = Translation;
            //return (piv * ((rot * sca) * -piv)) * tra;

            return tra * rot * sca;
        }
    }

    public Matrix3x3 Rotation
    {
        get
        {
            return new Matrix3x3(
                Mathf.Cos(rotation), -Mathf.Sin(rotation), 0,
                Mathf.Sin(rotation), Mathf.Cos(rotation), 0,
                0, 0, 1
            );
        }
    }

    public Matrix3x3 Scale
    {
        get
        {
            return new Matrix3x3(
                scale.x, 0, 0,
                0, scale.y, 0,
                0, 0, 1
            );
        }
    }
}
