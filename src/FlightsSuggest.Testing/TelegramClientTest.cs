﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FlightsSuggest.Core.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FlightsSuggest.Testing
{
    [TestFixture]
    public class TelegramClientTest : TestBase
    {
        [Test]
        public async Task TestSendMessageAsync()
        {
            var botClient = new TelegramBotClient(Configuration.TelegramBotToken);

            await botClient.SendTextMessageAsync(new ChatId(45921723), "он прячет карточки, ты носишь прыщи");
            
            var updates = await botClient.GetUpdatesAsync(0, limit: 100);

            foreach (var update in updates.Where(x => x.Message != null))
            {
                var messageText = update.Message.Text;
                if (!string.IsNullOrEmpty(messageText) &&
                    messageText.Contains("пора валить"))
                {
                    await botClient.SendTextMessageAsync(new ChatId("45921723"), $"лови ответочку от {update.Message.Chat.Username}: {messageText}");
                }
            }
        }

        [Test]
        public async Task TestGetUserAsync()
        {
            var telegramClient = Container.Container.Build().GetRequiredService<ITelegramClient>();
            var user = await telegramClient.GetUserAsync(45921723, 45921723);
            Console.WriteLine(JsonConvert.SerializeObject(user, Formatting.Indented));
        }
    }
}