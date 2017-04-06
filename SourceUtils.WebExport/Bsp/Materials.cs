﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ziks.WebServer;

namespace SourceUtils.WebExport.Bsp
{
    public class MaterialPage
    {
        public const int MaterialsPerPage = 2048;
        
        [JsonProperty("textures")]
        public List<Texture> Textures { get; } = new List<Texture>();

        [JsonProperty("materials")]
        public List<Material> Materials { get; } = new List<Material>();
    }

    [Prefix("/maps/{map}/materials")]
    class MaterialController : ResourceController
    {
        [Get("/matpage{index}.json")]
        public MaterialPage GetPage( [Url] string map, [Url] int index )
        {
            var bsp = Program.GetMap(map);
            var first = index * MaterialPage.MaterialsPerPage;
            var count = Math.Min(first + MaterialPage.MaterialsPerPage, bsp.TextureStringTable.Length) - first;

            if (count < 0)
            {
                first = bsp.TextureStringTable.Length;
                count = 0;
            }

            var texDict = new Dictionary<string, int>();

            var page = new MaterialPage();

            for ( var i = 0; i < count; ++i )
            {
                var texString = bsp.GetTextureString( first + i );
                var path = $"materials/{texString.ToLower()}.vmt".Replace('\\', '/');

                var mat = Material.Get( bsp, path );

                foreach ( var prop in mat.Properties )
                {
                    if ( prop.Type != MaterialPropertyType.TextureUrl ) continue;

                    prop.Type = MaterialPropertyType.TextureIndex;

                    var texUrl = (Url) prop.Value;
                    int texIndex;
                    if ( texDict.TryGetValue( texUrl, out texIndex ) )
                    {
                        prop.Value = texIndex;
                        continue;
                    }

                    prop.Value = texIndex = page.Textures.Count;

                    var texPath = TextureController.GetTexturePath( texUrl );
                    var tex = Texture.Get( bsp, texPath );

                    texDict.Add( texUrl, texIndex );
                    page.Textures.Add( tex );
                }

                page.Materials.Add( mat );
            }

            return page;
        }
    }
}