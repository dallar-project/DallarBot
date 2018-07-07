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

            //StringBuilder.AddField("Help", "");
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            this.StringBuilder.Append("Command: ")
                .AppendLine(Formatter.Bold(command.Name))
                .AppendLine();

            return this;
        }

        // this method is called sixth, it sets the current group's subcommands
        // if no group is being processed or current command is not a group, it 
        // won't be called
        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            this.StringBuilder.Append("Subcommands: ")
                .AppendLine(string.Join(", ", subcommands.Select(xc =>
                {
                    return $"{xc.Name} ({CategoryAttribute.GetCategory(xc.GetType())})";
                })))
                .AppendLine();

            return this;
        }

        // this is called as the last method, this should produce the final 
        // message, and return it

        public override CommandHelpMessage Build()
        {
            return new CommandHelpMessage(this.StringBuilder.ToString().Replace("\r\n", "\n"));
        }
    }
}