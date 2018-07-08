// THIS FILE IS A PART OF EMZI0767'S BOT EXAMPLES
//
// --------
// 
// Copyright 2017 Emzi0767
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// --------
//
// This is a commands example. It shows how to properly utilize 
// CommandsNext, as well as use its advanced functionality.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DallarBot.Classes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace DallarBot.Services
{
    // help formatters can alter the look of default help command,
    // this particular one replaces the embed with a simple text message.
    public class HelpFormatter : BaseHelpFormatter
    {
        private StringBuilder StringBuilder { get; }
        private DiscordEmbedBuilder EmbedBuilder { get; }

        public HelpFormatter(CommandContext ctx)
            : base(ctx)
        {
            StringBuilder = new StringBuilder();
            EmbedBuilder = new DiscordEmbedBuilder();

            EmbedBuilder.WithTitle("Help");
            EmbedBuilder.WithColor(DiscordColor.Blue);
            EmbedBuilder.WithAuthor(ctx.Client.CurrentUser.Username, "https://dallar.org", ctx.Client.CurrentUser.GetAvatarUrl(ImageFormat.Png, 64));
            EmbedBuilder.WithFooter("The Dallar Organization", ctx.Client.CurrentUser.GetAvatarUrl(ImageFormat.Png, 64));
            EmbedBuilder.WithTimestamp(DateTime.Now);
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            EmbedBuilder.WithDescription($"`{command.Name}`");

            float? Cost = ((CostAttribute)command.CustomAttributes.FirstOrDefault(a => a is CostAttribute))?.GetCost();

            if (Cost.GetValueOrDefault() != 0.0f)
            {
                EmbedBuilder.AddField("Cost", $"{Cost.GetValueOrDefault()} Dallar");
            }

            EmbedBuilder.AddField("Description", command.Description);

            if (command.Aliases.Count > 0)
            {
                EmbedBuilder.AddField("Aliases", string.Join("\n", command.Aliases.Select(s => $"`{s}`")));
            }
            
            if (command.Overloads[0].Arguments.Count > 0)
            {
                EmbedBuilder.AddField("Syntax", $"`{command.Name} {string.Join(" ", command.Overloads[0].Arguments.Select(arg => $"<{arg.Name}>"))}`");
                EmbedBuilder.AddField("Arguments", string.Join("\n", command.Overloads[0].Arguments.Select(arg => $"`<{arg.Name}>` - {arg.Description}")));
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            EmbedBuilder.WithDescription("You can get more specific help by also supplying the name of a command.");

            Dictionary<string, List<Command>> CategorizedCommands = new Dictionary<string, List<Command>>
            {
                // By pre-seeding we can control order
                { "Tipping", new List<Command>() },
                { "Exchange", new List<Command>() },
                { "Dallar", new List<Command>() },
                { "Jokes", new List<Command>() },
                { "Misc", new List<Command>() }
            };

            foreach (Command command in subcommands)
            {
                if (command.Name == "help")
                {
                    continue;
                }

                string Category = ((HelpCategoryAttribute)command.CustomAttributes.FirstOrDefault(a => a is HelpCategoryAttribute))?.GetCategory();
                if (Category == null)
                {
                    Category = "Uncategorized";
                }

                if (CategorizedCommands.ContainsKey(Category))
                {
                    CategorizedCommands[Category].Add(command);
                }
                else
                {
                    CategorizedCommands[Category] = new List<Command>(new Command[] { command });
                }
            }

            foreach (var Category in CategorizedCommands)
            {
                if (Category.Value.Count == 0)
                {
                    continue;
                }

                EmbedBuilder.AddField($"{Category.Key} Commands", string.Join("\n", Category.Value.Select(xc => $"`{xc.Name}` - {xc.Description}")));
            }

            return this;
        }

        // this is called as the last method, this should produce the final 
        // message, and return it

        public override CommandHelpMessage Build()
        {
            return new CommandHelpMessage(embed: EmbedBuilder.Build());
        }
    }
}