using System;
using UnityEngine;

[ExecuteInEditMode]
public class ASCIIRendering : MonoBehaviour
{


    public Shader ASCIIShader;
    private Material m_ASCII;


    public Texture2D CharTex;
    public int tilesX = 96;
    public int tilesY = 54;
    public int charWidth = 20;
    public int charHeight = 20;
    public int charCount = 8;
    public float brightness = .8f;
    public bool monochromatic = false;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        m_ASCII = new Material(ASCIIShader);
        m_ASCII.SetTexture("_CharTex", CharTex);

        m_ASCII.SetFloat("_tilesX", tilesX);
        m_ASCII.SetFloat("_tilesY", tilesY);
        m_ASCII.SetFloat("_tilesW", charWidth);
        m_ASCII.SetFloat("_tilesH", charHeight);
        m_ASCII.SetFloat("_charCount", charCount);
        m_ASCII.SetFloat("_monochromatic", (float)Convert.ToDouble(monochromatic));
        m_ASCII.SetFloat("_brightness", brightness);
        Graphics.Blit(source, destination, m_ASCII);
    }
}

