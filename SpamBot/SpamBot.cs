﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace SpamBot
{
	[ApiVersion(1, 16)]
	public class SpamBot : TerrariaPlugin
	{
		Config Config = new Config();
		DateTime[] Times = new DateTime[256];
		double[] Spams = new double[256];

		public override string Author
		{
			get { return "MarioE + Terrabear"; }
		}
		public override string Description
		{
			get { return "Spam Fighter"; }
		}
		public override string Name
		{
			get { return "SpamBot"; }
		}
		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		public SpamBot(Main game)
			: base(game)
		{
			Order = 999999;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				PlayerHooks.PlayerCommand -= OnPlayerCommand;
			}
		}
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetSendData.Register(this, OnSendData);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			PlayerHooks.PlayerCommand += OnPlayerCommand;
		}

		void OnChat(ServerChatEventArgs e)
		{
			if (!e.Handled)
			{
				string text = e.Text;
				if (e.Text.StartsWith(TShock.Config.CommandSpecifier))
					return;
				if ((DateTime.Now - Times[e.Who]).TotalSeconds > Config.Time)
				{
					Spams[e.Who] = 0.0;
					Times[e.Who] = DateTime.Now;
				}

				if (text.Trim().Length <= Config.ShortLength)
					Spams[e.Who] += Config.ShortWeight;
				else if ((double)text.Where(c => Char.IsUpper(c)).Count() / text.Length >= Config.CapsRatio)
					Spams[e.Who] += Config.CapsWeight;
				else
					Spams[e.Who] += Config.NormalWeight;

				if (Spams[e.Who] > Config.Threshold && !TShock.Players[e.Who].Group.HasPermission("spambot.ignore"))
				{
					switch (Config.Action.ToLower())
					{
						case "ignore":
						default:
							Times[e.Who] = DateTime.Now;
							TShock.Players[e.Who].SendErrorMessage("[SpamBot] You've been ignored for spamming. Wait 5 seconds to chat again.");
							e.Handled = true;
							return;
						case "kick":
                            TShock.Utils.ForceKick(TShock.Players[e.Who], "[SpamBot] You've been kicked for spamming. Do not spam again.", false, true);
							e.Handled = true;
							return;
					}
				}
			}
		}
		void OnInitialize(EventArgs e)
		{
			Commands.ChatCommands.Add(new Command("spambot.reload", Reload, "sbload"));

			string path = Path.Combine(TShock.SavePath, "spambot.json");
			if (File.Exists(path))
				Config = Config.Read(path);
			Config.Write(path);
		}
		void OnLeave(LeaveEventArgs e)
		{
			Spams[e.Who] = 0.0;
			Times[e.Who] = DateTime.Now;
		}
		void OnPlayerCommand(PlayerCommandEventArgs e)
		{
			if (!e.Handled && e.Player.RealPlayer)
			{
				switch (e.CommandName)
				{
					case "me":
					case "r":
					case "reply":
					case "tell":
					case "w":
					case "whisper":
						if ((DateTime.Now - Times[e.Player.Index]).TotalSeconds > Config.Time)
						{
							Spams[e.Player.Index] = 0.0;
							Times[e.Player.Index] = DateTime.Now;
						}

						string text = e.CommandText.Substring(e.CommandName.Length);
						if ((double)text.Where(c => Char.IsUpper(c)).Count() / text.Length >= Config.CapsRatio)
							Spams[e.Player.Index] += Config.CapsWeight;
						else if (text.Trim().Length <= Config.ShortLength)
							Spams[e.Player.Index] += Config.ShortWeight;
						else
							Spams[e.Player.Index] += Config.NormalWeight;

						if (Spams[e.Player.Index] > Config.Threshold && !TShock.Players[e.Player.Index].Group.HasPermission("antispam.ignore"))
						{
							switch (Config.Action.ToLower())
							{
								case "ignore":
								default:
									Times[e.Player.Index] = DateTime.Now;
                                    TShock.Players[e.Player.Index].SendErrorMessage("[SpamBot] You've been ignored for spamming. Wait 5 seconds to chat.");
									e.Handled = true;
									return;
								case "kick":
                                    TShock.Utils.ForceKick(TShock.Players[e.Player.Index], "[SpamBot] You've been kicked for spamming. Do not spam again.", false, true);
									e.Handled = true;
									return;
							}
						}
						return;
				}
			}
		}
		void OnSendData(SendDataEventArgs e)
		{
			if (e.MsgId == PacketTypes.ChatText && !e.Handled)
			{
                if (Config.DisableBossMessages == true && e.number2 == 175 && e.number3 == 75 && e.number4 == 255)
				{
					if (e.text.StartsWith("Eye of Cthulhu") || e.text.StartsWith("Eater of Worlds") ||
						e.text.StartsWith("Skeletron") || e.text.StartsWith("King Slime") ||
						e.text.StartsWith("The Destroyer") || e.text.StartsWith("The Twins") ||
						e.text.StartsWith("Skeletron Prime") || e.text.StartsWith("Wall of Flesh") ||
						e.text.StartsWith("Plantera") || e.text.StartsWith("Golem") || e.text.StartsWith("Brain of Cthulhu") ||
						e.text.StartsWith("Queen Bee") || e.text.StartsWith("Duke Fishron"))
					{
						e.Handled = true;
					}
				}
                if (Config.DisableMobMessages == true && e.number2 == 0 && e.number3 == 128 && e.number4 == 0)
                {
                    if (e.text.Contains("has spawned"))
                    {
                        e.Handled = true;
                    }
                }
                if (Config.DisableFwMessages == true && e.number2 == 0 && e.number3 == 128 && e.number4 == 0)
                {
                    if (e.text.StartsWith("Launched Firework"))
                    {
                        e.Handled = true;
                    }
                }
                /*if (Config.DisableNPCMessages == true && e.number2 == 255 && e.number3 == 0 && e.number4 == 0)
                {
                    if (e.text.Contains("was slain...") || e.text.Contains("has left!"))
                    {
                        e.Handled = true;
                    }
                }*/
                if (Config.DisablePvPMessages == true && e.number2 == 255 && e.number3 == 255 && e.number4 == 255)
                {
                    if (e.text.Contains("has enabled PvP!") || e.text.Contains("has disabled PvP!"))
                    {
                        e.Handled = true;
                    }
                }
                if (Config.DisableOrbMessages == true && e.number2 == 50 && e.number3 == 255 && e.number4 == 130)
				{
					if (e.text == "A horrible chill goes down your spine..." ||
						e.text == "Screams echo around you...")
					{
						e.Handled = true;
					}
				}
                if (Config.DisableSaveMessages == true && e.number2 == 255 && e.number3 == 255 && e.number4 == 0)
                {
                    if (e.text == "World saved.")
                    {
                        e.Handled = true;
                    }
                }
                if (Config.DisableSaveMessages == true && e.number2 == 0 && e.number3 == 128 && e.number4 == 0)
                {
                    if (e.text == "SSC has been saved." ||
                        e.text == "Save succeeded.")
                    {
                        e.Handled = true;
                    }
                }
			}
		}

		void Reload(CommandArgs e)
		{
			string path = Path.Combine(TShock.SavePath, "spambot.json");
			if (File.Exists(path))
				Config = Config.Read(path);
			Config.Write(path);
			e.Player.SendSuccessMessage("[SpamBot] Config reloaded.");
		}
	}
}