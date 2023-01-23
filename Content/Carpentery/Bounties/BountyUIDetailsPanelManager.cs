﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Aequus.Content.Carpentery.Bounties
{
    public class BountyUIDetailsPanelManager : UIElement
    {
        public readonly TestBountyUIState parentState;
        public readonly UIPanel uiPanel;

        public CarpenterBounty bounty;

        public BountyUIDetailsPanelManager(TestBountyUIState parent, UIPanel panel)
        {
            parentState = parent;
            uiPanel = panel;
        }

        public void SetBounty(CarpenterBounty bounty)
        {
            Clear();
            var uiText = new UIText(bounty.DisplayName, 0.7f, large: true);
            uiText.HAlign = 0.5f;
            uiText.Top.Set(8, 0f);
            uiPanel.Append(uiText);

            var descriptionPanel = new UIPanel();
            descriptionPanel.BackgroundColor = new Color(91, 124, 193) * 0.9f;
            descriptionPanel.BorderColor = uiPanel.BackgroundColor * 0.5f;
            descriptionPanel.Top.Set(42f, 0f);
            descriptionPanel.Width.Set(0f, 1f);
            descriptionPanel.Height.Set(100f, 0.1f);
            uiPanel.Append(descriptionPanel);
            uiText = new UIText($"{bounty.Description}", 1f)
            {
                HAlign = 0f,
                VAlign = 0f,
                TextOriginX = 0f,
                Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
                Height = StyleDimension.FromPixelsAndPercent(0f, 1f),
                IgnoresMouseInteraction = true,
                IsWrapped = true,
            };
            descriptionPanel.Append(uiText);
            uiText.FixUIText();

            var npcHead = TextureAssets.NpcHead[NPC.TypeToDefaultHeadIndex(bounty.GetBountyNPCID())];
            var uiImage = new UIImage(npcHead)
            {
                ImageScale = 1.05f
            };
            uiImage.Width.Set(npcHead.Value.Width, 0f);
            uiImage.Height.Set(npcHead.Value.Height, 0f);
            uiImage.Left.Set(32 - npcHead.Value.Width / 2, 0f);
            uiImage.Top.Set(20 - npcHead.Value.Height / 2, 0f);
            uiPanel.Append(uiImage);

            var stepsPanel = new UIPanel();
            stepsPanel.BackgroundColor = new Color(91, 124, 193) * 0.9f;
            stepsPanel.BorderColor = stepsPanel.BackgroundColor * 1.3f;
            stepsPanel.Top.Set(42f + 100f + 8f, descriptionPanel.Height.Percent);
            stepsPanel.Width.Set(-10f, 0.5f);
            stepsPanel.Height.Set(0f, 0.45f);
            uiPanel.Append(stepsPanel);

            var text = "";
            foreach (var t in bounty.StepsToString())
            {
                if (!string.IsNullOrEmpty(text))
                    text += "\n";
                text += $"- {t}";
            }
            uiText = new UIText(text, 0.8f)
            {
                HAlign = 0f,
                VAlign = 0f,
                TextOriginX = 0f,
                Width = StyleDimension.FromPixelsAndPercent(0f, 1f),
                Height = StyleDimension.FromPixelsAndPercent(0f, 1f),
                IgnoresMouseInteraction = true,
                IsWrapped = true,
            };
            stepsPanel.Append(uiText);
            uiText.FixUIText();

            var blueprintPanel = new UIPanel();
            blueprintPanel.BackgroundColor = (new Color(91, 124, 193) * 0.5f).UseA(255) * 0.9f;
            blueprintPanel.BorderColor = stepsPanel.BackgroundColor * 1.3f;
            blueprintPanel.Left = stepsPanel.Width;
            blueprintPanel.Left.Pixels += 16f;
            blueprintPanel.Top = stepsPanel.Top;
            blueprintPanel.Width.Set(-10f, 0.5f);
            blueprintPanel.Height.Set(0f, 0.45f);
            uiPanel.Append(blueprintPanel);
            
            var top = stepsPanel.Top;
            top.Pixels += 10f;
            top.Percent += 0.45f;
            var buttonPanel = new UIPanel();
            buttonPanel.BackgroundColor = stepsPanel.BackgroundColor;
            buttonPanel.BorderColor = stepsPanel.BackgroundColor * 1.3f;
            buttonPanel.Left = stepsPanel.Left;
            buttonPanel.Top = top;
            buttonPanel.Width.Set(-10f, 0.33f);
            buttonPanel.Height.Set(64f, 0f);
            uiPanel.Append(buttonPanel);

            uiText = new UIText("Buy Materials");
            uiText.DynamicallyScaleDownToWidth = true;
            uiText.HAlign = 0.5f;
            uiText.VAlign = 0.5f;
            buttonPanel.Append(uiText);
            
            buttonPanel = new UIPanel();
            buttonPanel.BackgroundColor = stepsPanel.BackgroundColor;
            buttonPanel.BorderColor = stepsPanel.BackgroundColor * 1.3f;
            buttonPanel.Left = stepsPanel.Left;
            buttonPanel.Left.Precent += 0.33f;
            buttonPanel.Top = top;
            buttonPanel.Width.Set(-10f, 0.33f);
            buttonPanel.Height.Set(64f, 0f);
            uiPanel.Append(buttonPanel);

            uiText = new UIText("Set as Quest");
            uiText.DynamicallyScaleDownToWidth = true;
            uiText.HAlign = 0.5f;
            uiText.VAlign = 0.5f;
            buttonPanel.Append(uiText);

            var rewardPanel = new UIPanel();
            rewardPanel.BackgroundColor = stepsPanel.BackgroundColor;
            rewardPanel.BorderColor = stepsPanel.BackgroundColor * 1.3f;
            rewardPanel.Left = stepsPanel.Left;
            rewardPanel.Left.Precent += 0.66f;
            rewardPanel.Top = top;
            rewardPanel.Width.Set(-10f, 0.33f);
            rewardPanel.Height.Set(-rewardPanel.Top.Pixels, 1f - rewardPanel.Top.Precent);
            uiPanel.Append(rewardPanel);

            uiText = new UIText("Reward:", 0.56f, large: true);
            uiText.DynamicallyScaleDownToWidth = true;
            uiText.Top.Set(6f, 0f);
            uiText.HAlign = 0.5f;
            rewardPanel.Append(uiText);
        }

        public void Clear()
        {
            bounty = null;
            uiPanel.RemoveAllChildren();
        }
    }
}