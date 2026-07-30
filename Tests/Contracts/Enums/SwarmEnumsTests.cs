using System;
using System.Collections.Generic;
using SwarmUI.ApiClient.Contracts.Enums;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Models.Enums
{
    /// <summary>Unit tests for <see cref="SwarmSubType"/>, <see cref="SwarmModelAuthor"/>, and <see cref="SwarmModelFamily"/> extension methods.</summary>
    public class SwarmEnumsTests
    {
        [Theory]
        [InlineData(SwarmSubType.StableDiffusion, "Stable-Diffusion", "Stable-Diffusion")]
        [InlineData(SwarmSubType.VAE, "VAE", "VAE")]
        [InlineData(SwarmSubType.LoRA, "LoRA", "Lora")]
        [InlineData(SwarmSubType.Embedding, "Embedding", "Embeddings")]
        [InlineData(SwarmSubType.ControlNet, "ControlNet", "controlnet")]
        [InlineData(SwarmSubType.Clip, "Clip", "text_encoders")]
        [InlineData(SwarmSubType.ClipVision, "ClipVision", "clip_vision")]
        [InlineData(SwarmSubType.LLM, "LLM", "llm")]
        public void SwarmSubType_AsApiType_And_AsFolder_MatchSwarmUICanonicalStrings(SwarmSubType subType, string expectedApi, string expectedFolder)
        {
            Assert.Equal(expectedApi, subType.AsApiType());
            Assert.Equal(expectedFolder, subType.AsFolder());
        }

        [Theory]
        [InlineData("Stable-Diffusion", SwarmSubType.StableDiffusion)]
        [InlineData("VAE", SwarmSubType.VAE)]
        [InlineData("LoRA", SwarmSubType.LoRA)]
        [InlineData("Embedding", SwarmSubType.Embedding)]
        [InlineData("ControlNet", SwarmSubType.ControlNet)]
        [InlineData("Clip", SwarmSubType.Clip)]
        [InlineData("ClipVision", SwarmSubType.ClipVision)]
        [InlineData("LLM", SwarmSubType.LLM)]
        public void SwarmSubType_TryParseApiType_AcceptsAllCanonicalStrings(string apiType, SwarmSubType expected)
        {
            bool parsed = SwarmSubTypeExtensions.TryParseApiType(apiType, out SwarmSubType result);
            Assert.True(parsed);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("lora")]
        [InlineData("stable-diffusion")]
        [InlineData("unknown")]
        [InlineData("")]
        public void SwarmSubType_TryParseApiType_RejectsUnknownOrMiscasedStrings(string apiType)
        {
            bool parsed = SwarmSubTypeExtensions.TryParseApiType(apiType, out SwarmSubType _);
            Assert.False(parsed);
        }

        [Fact]
        public void SwarmSubType_AsApiType_RoundTripsThroughTryParse()
        {
            foreach (SwarmSubType subType in Enum.GetValues<SwarmSubType>())
            {
                bool parsed = SwarmSubTypeExtensions.TryParseApiType(subType.AsApiType(), out SwarmSubType result);
                Assert.True(parsed, $"AsApiType()/TryParseApiType round trip failed for {subType}");
                Assert.Equal(subType, result);
            }
        }

        [Fact]
        public void SwarmModelFamily_GetAuthor_DefinedForEveryEnumValue()
        {
            foreach (SwarmModelFamily family in Enum.GetValues<SwarmModelFamily>())
            {
                SwarmModelAuthor author = family.GetAuthor();
                Assert.True(Enum.IsDefined(typeof(SwarmModelAuthor), author), $"Family {family} mapped to undefined author {author}");
            }
        }

        [Theory]
        [InlineData(SwarmModelFamily.SDXL, SwarmModelAuthor.StabilityAI)]
        [InlineData(SwarmModelFamily.Flux1, SwarmModelAuthor.BFL)]
        [InlineData(SwarmModelFamily.Flux2, SwarmModelAuthor.BFL)]
        [InlineData(SwarmModelFamily.ZImage, SwarmModelAuthor.Alibaba)]
        [InlineData(SwarmModelFamily.QwenImage, SwarmModelAuthor.Alibaba)]
        [InlineData(SwarmModelFamily.Wan, SwarmModelAuthor.Alibaba)]
        [InlineData(SwarmModelFamily.HunyuanImage, SwarmModelAuthor.Tencent)]
        [InlineData(SwarmModelFamily.HunyuanVideo, SwarmModelAuthor.Tencent)]
        [InlineData(SwarmModelFamily.LTXV2, SwarmModelAuthor.Lightricks)]
        [InlineData(SwarmModelFamily.Kandinsky5Image, SwarmModelAuthor.KandinskyLab)]
        [InlineData(SwarmModelFamily.ChromaRadiance, SwarmModelAuthor.LodestoneRock)]
        [InlineData(SwarmModelFamily.AuraFlow, SwarmModelAuthor.FalAI)]
        [InlineData(SwarmModelFamily.Lumina, SwarmModelAuthor.AlphaVLLM)]
        [InlineData(SwarmModelFamily.Anima, SwarmModelAuthor.CirclestoneLabs)]
        [InlineData(SwarmModelFamily.ERNIE, SwarmModelAuthor.Baidu)]
        [InlineData(SwarmModelFamily.SSD1B, SwarmModelAuthor.Segmind)]
        [InlineData(SwarmModelFamily.PixArtSigma, SwarmModelAuthor.PixArt)]
        [InlineData(SwarmModelFamily.Sana, SwarmModelAuthor.NVIDIA)]
        [InlineData(SwarmModelFamily.CosmosPredict2, SwarmModelAuthor.NVIDIA)]
        [InlineData(SwarmModelFamily.HiDreamI1, SwarmModelAuthor.HiDreamAI)]
        [InlineData(SwarmModelFamily.HiDreamO1, SwarmModelAuthor.HiDreamAI)]
        [InlineData(SwarmModelFamily.OmniGen2, SwarmModelAuthor.VectorSpaceLab)]
        [InlineData(SwarmModelFamily.AceStep, SwarmModelAuthor.StepFun)]
        [InlineData(SwarmModelFamily.Mochi1, SwarmModelAuthor.Genmo)]
        public void SwarmModelFamily_GetAuthor_ReturnsExpectedAuthor(SwarmModelFamily family, SwarmModelAuthor expected)
        {
            Assert.Equal(expected, family.GetAuthor());
        }

        [Fact]
        public void SwarmModelFamily_AllStabilityAIFamiliesGrouped()
        {
            HashSet<SwarmModelFamily> stabilityAI = new HashSet<SwarmModelFamily>
            {
                SwarmModelFamily.SD1, SwarmModelFamily.SD2, SwarmModelFamily.SDXL, SwarmModelFamily.SDTurbo,
                SwarmModelFamily.SD3, SwarmModelFamily.SD35, SwarmModelFamily.StableCascade
            };
            foreach (SwarmModelFamily family in stabilityAI)
            {
                Assert.Equal(SwarmModelAuthor.StabilityAI, family.GetAuthor());
            }
        }
    }
}
