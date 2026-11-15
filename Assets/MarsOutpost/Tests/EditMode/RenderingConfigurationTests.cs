using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarsOutpost.Tests
{
    public class RenderingConfigurationTests
    {
        [Test]
        public void PipelineUses3DRendererAndSupportsMainLightShadows()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            Assert.That(pipeline, Is.Not.Null);
            var settings = new SerializedObject(pipeline);
            var rendererList = settings.FindProperty("m_RendererDataList");
            int index = settings.FindProperty("m_DefaultRendererIndex").intValue;
            Assert.That(index, Is.InRange(0, rendererList.arraySize - 1));
            var renderer = rendererList.GetArrayElementAtIndex(index).objectReferenceValue;
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.GetType().Name, Is.EqualTo("UniversalRendererData"));
            var rendererProperty = pipeline.GetType().GetProperty("scriptableRenderer");
            Assert.That(rendererProperty, Is.Not.Null);
            Assert.That(rendererProperty.GetValue(pipeline), Is.Not.Null, "The configured renderer must instantiate.");
            Assert.That(settings.FindProperty("m_MainLightShadowsSupported").boolValue, Is.True);
            Assert.That(settings.FindProperty("m_SoftShadowsSupported").boolValue, Is.True);
        }
    }
}
