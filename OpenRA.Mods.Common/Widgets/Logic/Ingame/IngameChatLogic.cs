#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("OpenTeamChat", "OpenGeneralChat")]
	public class IngameChatLogic : ChromeLogic
	{
		readonly OrderManager orderManager;
		readonly Ruleset modRules;

		readonly ContainerWidget chatOverlay;
		readonly ChatDisplayWidget chatOverlayDisplay;

		readonly ContainerWidget chatChrome;
		readonly ScrollPanelWidget chatScrollPanel;
		readonly ContainerWidget chatTemplate;
		readonly TextFieldWidget chatText;
		readonly CachedTransform<int, string> chatDisabledLabel;

		readonly TabCompletionLogic tabCompletion = new TabCompletionLogic();

		readonly string chatLineSound = ChromeMetrics.Get<string>("ChatLineSound");

		TextNotification lastLine;
		int repetitions;
		bool chatEnabled = true;

		[ObjectCreator.UseCtor]
		public IngameChatLogic(Widget widget, OrderManager orderManager, World world, ModData modData, bool isMenuChat, Dictionary<string, MiniYaml> logicArgs)
		{
			this.orderManager = orderManager;
			modRules = modData.DefaultRules;

			var chatTraits = world.WorldActor.TraitsImplementing<INotifyChat>().ToArray();
			var players = world.Players.Where(p => p != world.LocalPlayer && !p.NonCombatant && !p.IsBot);
			var isObserver = orderManager.LocalClient != null && orderManager.LocalClient.IsObserver;
			var alwaysDisabled = world.IsReplay || world.LobbyInfo.NonBotClients.Count() == 1;
			var disableTeamChat = alwaysDisabled || (world.LocalPlayer != null && !players.Any(p => p.IsAlliedWith(world.LocalPlayer)));
			var teamChat = !disableTeamChat;

			tabCompletion.Commands = chatTraits.OfType<ChatCommands>().SelectMany(x => x.Commands.Keys).ToList();
			tabCompletion.Names = orderManager.LobbyInfo.Clients.Select(c => c.Name).Distinct().ToList();

			var chatPanel = (ContainerWidget)widget;
			chatOverlay = chatPanel.GetOrNull<ContainerWidget>("CHAT_OVERLAY");
			if (chatOverlay != null)
			{
				chatOverlayDisplay = chatOverlay.Get<ChatDisplayWidget>("CHAT_DISPLAY");
				chatOverlay.Visible = false;
			}

			chatChrome = chatPanel.Get<ContainerWidget>("CHAT_CHROME");
			chatChrome.Visible = true;

			var chatMode = chatChrome.Get<ButtonWidget>("CHAT_MODE");
			chatMode.GetText = () => teamChat && !disableTeamChat ? "Team" : "All";
			chatMode.OnClick = () => teamChat ^= true;

			// Enable teamchat if we are a player and die,
			// or disable it when we are the only one left in the team
			if (!alwaysDisabled && world.LocalPlayer != null)
			{
				chatMode.IsDisabled = () =>
				{
					if (world.IsGameOver || !chatEnabled)
						return true;

					// The game is over for us, join spectator team chat
					if (world.LocalPlayer.WinState != WinState.Undefined)
					{
						disableTeamChat = false;
						return disableTeamChat;
					}

					// If team chat isn't already disabled, check if we are the only living team member
					if (!disableTeamChat)
						disableTeamChat = players.All(p => p.WinState != WinState.Undefined || !p.IsAlliedWith(world.LocalPlayer));

					return disableTeamChat;
				};
			}
			else
				chatMode.IsDisabled = () => disableTeamChat || !chatEnabled;

			// Disable team chat after the game ended
			world.GameOver += () => disableTeamChat = true;

			chatText = chatChrome.Get<TextFieldWidget>("CHAT_TEXTFIELD");
			chatText.MaxLength = UnitOrders.ChatMessageMaxLength;
			chatText.OnEnterKey = _ =>
			{
				var team = teamChat && !disableTeamChat;
				if (chatText.Text != "")
				{
					if (!chatText.Text.StartsWith("/", StringComparison.Ordinal))
					{
						// This should never happen, but avoid a crash if it does somehow (chat will just stay open)
						if (!isObserver && orderManager.LocalClient == null && world.LocalPlayer == null)
							return true;

						var teamNumber = 0U;
						if (team)
							teamNumber = (isObserver || world.LocalPlayer.WinState != WinState.Undefined) ? uint.MaxValue : (uint)orderManager.LocalClient.Team;

						orderManager.IssueOrder(Order.Chat(chatText.Text.Trim(), teamNumber));
					}
					else if (chatTraits != null)
					{
						var text = chatText.Text.Trim();
						var from = world.IsReplay ? null : orderManager.LocalClient.Name;
						foreach (var trait in chatTraits)
							trait.OnChat(from, text);
					}
				}

				chatText.Text = "";
				if (!isMenuChat)
					CloseChat();

				return true;
			};

			chatText.OnTabKey = e =>
			{
				if (!chatMode.Key.IsActivatedBy(e) || chatMode.IsDisabled())
				{
					chatText.Text = tabCompletion.Complete(chatText.Text);
					chatText.CursorPosition = chatText.Text.Length;
				}
				else
					chatMode.OnKeyPress(e);

				return true;
			};

			chatText.OnEscKey = _ =>
			{
				if (!isMenuChat)
					CloseChat();
				else
					chatText.YieldKeyboardFocus();

				return true;
			};

			chatDisabledLabel = new CachedTransform<int, string>(x => x > 0 ? $"Chat available in {x} seconds..." : "Chat Disabled");

			if (!isMenuChat)
			{
				var openTeamChatKey = new HotkeyReference();
				if (logicArgs.TryGetValue("OpenTeamChatKey", out var hotkeyArg))
					openTeamChatKey = modData.Hotkeys[hotkeyArg.Value];

				var openGeneralChatKey = new HotkeyReference();
				if (logicArgs.TryGetValue("OpenGeneralChatKey", out hotkeyArg))
					openGeneralChatKey = modData.Hotkeys[hotkeyArg.Value];

				var chatClose = chatChrome.Get<ButtonWidget>("CHAT_CLOSE");
				chatClose.OnClick += CloseChat;

				chatPanel.OnKeyPress = e =>
				{
					if (e.Event == KeyInputEvent.Up)
						return false;

					if (!chatChrome.IsVisible() && (openTeamChatKey.IsActivatedBy(e) || openGeneralChatKey.IsActivatedBy(e)))
					{
						teamChat = !disableTeamChat && !openGeneralChatKey.IsActivatedBy(e);

						OpenChat();
						return true;
					}

					return false;
				};
			}

			chatScrollPanel = chatChrome.Get<ScrollPanelWidget>("CHAT_SCROLLPANEL");
			chatTemplate = chatScrollPanel.Get<ContainerWidget>("CHAT_TEMPLATE");
			chatScrollPanel.RemoveChildren();
			chatScrollPanel.ScrollToBottom();

			foreach (var chatLine in orderManager.NotificationsCache)
				AddChatLine(chatLine, true);

			orderManager.AddTextNotification += AddChatLineWrapper;

			chatText.IsDisabled = () => !chatEnabled || (world.IsReplay && !Game.Settings.Debug.EnableDebugCommandsInReplays);

			if (!isMenuChat)
			{
				CloseChat();

				var keyListener = chatChrome.Get<LogicKeyListenerWidget>("KEY_LISTENER");
				keyListener.AddHandler(e =>
				{
					if (e.Event == KeyInputEvent.Up || !chatText.IsDisabled())
						return false;

					if ((e.Key == Keycode.RETURN || e.Key == Keycode.KP_ENTER || e.Key == Keycode.ESCAPE) && e.Modifiers == Modifiers.None)
					{
						CloseChat();
						return true;
					}

					return false;
				});
			}

			if (logicArgs.TryGetValue("ChatLineSound", out var yaml))
				chatLineSound = yaml.Value;
		}

		public void OpenChat()
		{
			chatText.Text = "";
			chatChrome.Visible = true;
			chatScrollPanel.ScrollToBottom();
			if (!chatText.IsDisabled())
				chatText.TakeKeyboardFocus();

			chatOverlay.Visible = false;
		}

		public void CloseChat()
		{
			chatChrome.Visible = false;
			chatText.YieldKeyboardFocus();
			chatOverlay.Visible = true;
			Ui.ResetTooltips();
		}

		public void AddChatLineWrapper(TextNotification chatLine)
		{
			var chatLineToDisplay = chatLine;

			if (chatLine.CanIncrementOnDuplicate() && chatLine.Equals(lastLine))
			{
				repetitions++;
				chatLineToDisplay = new TextNotification(
					chatLine.Pool,
					chatLine.Prefix,
					$"{chatLine.Text} ({repetitions + 1})",
					chatLine.PrefixColor,
					chatLine.TextColor);

				chatScrollPanel.RemoveChild(chatScrollPanel.Children[chatScrollPanel.Children.Count - 1]);
				chatOverlayDisplay?.RemoveMostRecentLine();
			}
			else
				repetitions = 0;

			lastLine = chatLine;

			chatOverlayDisplay?.AddLine(chatLineToDisplay);

			// HACK: Force disable the chat notification sound for the in-menu chat dialog
			// This works around our inability to disable the sounds for the in-game dialog when it is hidden
			AddChatLine(chatLineToDisplay, chatOverlay == null);
		}

		void AddChatLine(TextNotification chatLine, bool suppressSound)
		{
			var template = chatTemplate.Clone();
			var nameLabel = template.Get<LabelWidget>("NAME");
			var textLabel = template.Get<LabelWidget>("TEXT");

			var name = "";
			if (!string.IsNullOrEmpty(chatLine.Prefix))
				name = chatLine.Prefix + ":";

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var nameSize = font.Measure(chatLine.Prefix);

			nameLabel.GetColor = () => chatLine.PrefixColor;
			nameLabel.GetText = () => name;
			nameLabel.Bounds.Width = nameSize.X;

			textLabel.GetColor = () => chatLine.TextColor;
			textLabel.Bounds.X += nameSize.X;
			textLabel.Bounds.Width -= nameSize.X;

			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			var text = WidgetUtils.WrapText(chatLine.Text, textLabel.Bounds.Width, font);
			textLabel.GetText = () => text;
			var dh = font.Measure(text).Y - textLabel.Bounds.Height;
			if (dh > 0)
			{
				textLabel.Bounds.Height += dh;
				template.Bounds.Height += dh;
			}

			var scrolledToBottom = chatScrollPanel.ScrolledToBottom;
			chatScrollPanel.AddChild(template);
			if (scrolledToBottom)
				chatScrollPanel.ScrollToBottom(smooth: true);

			if (!suppressSound)
				Game.Sound.PlayNotification(modRules, null, "Sounds", chatLineSound, null);
		}

		public override void Tick()
		{
			var chatWasEnabled = chatEnabled;
			chatEnabled = Game.RunTime >= TextNotificationsManager.ChatDisabledUntil && TextNotificationsManager.ChatDisabledUntil != uint.MaxValue;

			if (chatEnabled && !chatWasEnabled)
			{
				chatText.Text = "";
				if (Ui.KeyboardFocusWidget == null)
					chatText.TakeKeyboardFocus();
			}
			else if (!chatEnabled)
			{
				var remaining = 0;
				if (TextNotificationsManager.ChatDisabledUntil != uint.MaxValue)
					remaining = (int)(TextNotificationsManager.ChatDisabledUntil - Game.RunTime + 999) / 1000;

				chatText.Text = chatDisabledLabel.Update(remaining);
			}
		}

		bool disposed = false;
		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				orderManager.AddTextNotification -= AddChatLineWrapper;
				disposed = true;
			}

			base.Dispose(disposing);
		}
	}
}
