﻿using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Cesium
{
    using VBTexture1 = VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>;


    [Category("Toolkit.Scenes")]
    public class ExtMeshFeaturesTests
    {
        [SetUp]
        public void SetUp()
        {
            CesiumExtensions.RegisterExtensions();
        }

        [Test(Description = "Test for settting the FeatureIds with vertex attributes. See sample https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_mesh_features/FeatureIdAttribute")]
        public void FeaturesIdAttributeTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Create a triangle with feature ID custom vertex attribute
            var featureId = 1;
            var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

            var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
            var prim = mesh.UsePrimitive(material);

            // All the vertices in the triangle have the same feature ID
            var vt0 = GetVertexBuilderWithFeatureId(new Vector3(-10, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt1 = GetVertexBuilderWithFeatureId(new Vector3(10, 0, 0), new Vector3(0, 0, 1), featureId);
            var vt2 = GetVertexBuilderWithFeatureId(new Vector3(0, 10, 0), new Vector3(0, 0, 1), featureId);

            prim.AddTriangle(vt0, vt1, vt2);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            var featureIdAttribute = new MeshExtMeshFeatureID(1, 0);

            // Set the FeatureIds
            var featureIds = new List<MeshExtMeshFeatureID>() { featureIdAttribute };
            model.LogicalMeshes[0].Primitives[0].SetFeatureIds(featureIds);

            // Validate the FeatureIds
            var cesiumExtMeshFeaturesExtension = (MeshExtMeshFeatures)model.LogicalMeshes[0].Primitives[0].Extensions.FirstOrDefault();
            Assert.NotNull(cesiumExtMeshFeaturesExtension.FeatureIds);

            Assert.IsTrue(cesiumExtMeshFeaturesExtension.FeatureIds.Equals(featureIds));

            // Check there should be a custom vertex attribute with name _FEATURE_ID_{attribute}
            var attribute = cesiumExtMeshFeaturesExtension.FeatureIds[0].Attribute;
            Assert.IsTrue(attribute == 0);
            var primitive = model.LogicalMeshes[0].Primitives[0];
            var featureIdVertexAccessor = primitive.GetVertexAccessor($"_FEATURE_ID_{attribute}");
            Assert.NotNull(featureIdVertexAccessor);
            var items = featureIdVertexAccessor.AsScalarArray();
            Assert.AreEqual(items, new List<int> { featureId, featureId, featureId });

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.ValidateContent(ctx.GetContext());
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_attribute.plotly");
        }

        [Test(Description = "Test for settting the FeatureIds with a texture. See sample https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_mesh_features/FeatureIdTexture")]
        public void FeaturesIdTextureTest()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // Bitmap of 16*16 pixels, containing FeatureID's (0, 1, 2, 3) in the red channel
            var img0 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAJElEQVR42mNgYmBgoAQzDLwBgwcwY8FDzIDBDRiR8KgBNDAAAOKBAKByX2jMAAAAAElFTkSuQmCC";
            var imageBytes = Convert.FromBase64String(img0);
            var imageBuilder = ImageBuilder.From(imageBytes);

            var material = MaterialBuilder
                .CreateDefault()
                .WithMetallicRoughnessShader()
                .WithBaseColor(imageBuilder, new Vector4(1, 1, 1, 1))
                .WithDoubleSide(true)
                .WithAlpha(Materials.AlphaMode.OPAQUE)
                .WithMetallicRoughness(0, 1);

            var mesh = VBTexture1.CreateCompatibleMesh("mesh");
            var prim = mesh.UsePrimitive(material);
            prim.AddTriangle(
                new VBTexture1(new VertexPosition(0, 0, 0), new Vector2(0, 1)),
                new VBTexture1(new VertexPosition(1, 0, 0), new Vector2(1, 1)),
                new VBTexture1(new VertexPosition(0, 1, 0), new Vector2(0, 0)));

            prim.AddTriangle(
                new VBTexture1(new VertexPosition(1, 0, 0), new Vector2(1, 1)),
                new VBTexture1(new VertexPosition(1, 1, 0), new Vector2(1, 0)),
                new VBTexture1(new VertexPosition(0, 1, 0), new Vector2(0, 0)));

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            var model = scene.ToGltf2();

            // Set the FeatureIds, pointing to the red channel of the texture
            var texture = new MeshExtMeshFeatureIDTexture(new List<int>() { 0 }, 0, 0);
            var featureIdTexture = new MeshExtMeshFeatureID(4, texture: texture);
            var featureIds = new List<MeshExtMeshFeatureID>() { featureIdTexture };
            var primitive = model.LogicalMeshes[0].Primitives[0];
            primitive.SetFeatureIds(featureIds);

            var cesiumExtMeshFeaturesExtension = (MeshExtMeshFeatures)primitive.Extensions.FirstOrDefault();
            Assert.NotNull(cesiumExtMeshFeaturesExtension.FeatureIds);

            Assert.IsTrue(cesiumExtMeshFeaturesExtension.FeatureIds.Equals(featureIds));
            var featureId = cesiumExtMeshFeaturesExtension.FeatureIds[0];
            var texCoord = featureId.Texture.TextureCoordinate;

            var textureIdVertexAccessor = primitive.GetVertexAccessor($"TEXCOORD_{texCoord}");
            Assert.NotNull(textureIdVertexAccessor);
            Assert.IsTrue(textureIdVertexAccessor.AsVector2Array().Count == 4);

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);

            model.ValidateContent(ctx.GetContext());
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_texture.glb");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_texture.gltf");
            scene.AttachToCurrentTest("cesium_ext_mesh_features_feature_id_texture.plotly");
        }

        private static VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty> GetVertexBuilderWithFeatureId(Vector3 position, Vector3 normal, int featureid)
        {
            var vp0 = new VertexPositionNormal(position, normal);
            var vb0 = new VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>(vp0, featureid);
            return vb0;
        }
    }
}