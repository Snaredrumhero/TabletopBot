using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace TableTopBot
{
    public class MultiPageEmbed
    {
        private readonly RestFollowupMessage _response;
        private readonly List<EmbedBuilder>? _pages;

        public int PageNumber
        {
            get;
            private set;
        }

        public bool IsLastPage => !TryGetPage(PageNumber + 1, out _);

        public async Task UpdatePage(int pageNumber)
        {
            EmbedBuilder builder = GetPage(pageNumber);
            PageNumber = pageNumber;
            await _response.ModifyAsync(z =>
            {
                z.Embeds = new Optional<Embed[]>(new[] { builder.Build() });
                z.Components = new Optional<MessageComponent>(GetButtons(PageNumber == 0, IsLastPage).Build());
            });
        }

        public async Task NextPage()
        {
            await UpdatePage(PageNumber + 1);
        }

        public async Task PreviousPage()
        {
            await UpdatePage(PageNumber - 1);
        }

        public EmbedBuilder GetPage(int pageNumber)
        {
            if (_customGetPage is not null)
            {
                return _customGetPage(pageNumber);
            }

            if (pageNumber < 0 || pageNumber >= _pages!.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            return _pages[pageNumber];
        }

        public bool TryGetPage(int pageNumber, out EmbedBuilder? builder)
        {
            try
            {
                builder = GetPage(pageNumber);
                return true;
            }
            catch (Exception)
            {
                builder = null;
                return false;
            }
        }

        private readonly Func<int, EmbedBuilder>? _customGetPage;

        private static ComponentBuilder GetButtons(bool isBackDisabled, bool isNextDisabled)
        {
            ComponentBuilder c = new();
            c.WithButton(ButtonBuilder.CreatePrimaryButton("Previous Page", "back-button").WithDisabled(isBackDisabled));
            c.WithButton(ButtonBuilder.CreatePrimaryButton("Next Page", "next-button").WithDisabled(isNextDisabled));
            return c;
        }

        /// <param name="interaction">
        /// The interaction the embeds are intended for. Calling this constructor will send a Followup
        /// </param>
        /// <param name="pages">
        /// A <c>List</c> of <c>EmbedBuilders</c> to serve as pregenerated pages
        /// </param>
        /// <param name="text">The text body of the message (separate from the embeds)</param>
        /// <param name="isTts">Is response Text to Speech</param>
        /// <param name="ephemeral">True if only the user who triggered the interaction can see the followup, false otherwise</param>
        /// <param name="allowedMentions">The allowed mentions for the followup</param>
        /// <param name="options">The request options for the followup</param>
        public MultiPageEmbed(SocketInteraction interaction, IEnumerable<EmbedBuilder> pages, string? text = null, bool isTts = false,
            bool ephemeral = false, AllowedMentions? allowedMentions = null, RequestOptions? options = null)
        {
            if (!interaction.HasResponded)
            {
                interaction.DeferAsync(ephemeral: true).GetAwaiter().GetResult();
            }

            _pages = pages.ToList();

            if (_pages.Count == 0)
            {
                throw new InvalidOperationException("Pages must contain at least one item");
            }

            ComponentBuilder c = GetButtons(true, false);

            _response = interaction.FollowupAsync(embed: _pages.First().Build(), text: text, isTTS: isTts, ephemeral: ephemeral, allowedMentions: allowedMentions, 
                components: c.Build(), options: options, embeds: null).GetAwaiter().GetResult();
            _customGetPage = null;
            PageNumber = 0;
        }

        /// <param name="interaction">
        /// The interaction the embeds are intended for. Calling this constructor will send a Followup
        /// </param>
        /// <param name="getPageFunction">
        /// A function that will return an EmbedBuilder after being supplied a page number.
        /// </param>
        /// <param name="text">The text body of the message (separate from the embeds)</param>
        /// <param name="isTts">Is response Text to Speech</param>
        /// <param name="ephemeral">True if only the user who triggered the interaction can see the followup, false otherwise</param>
        /// <param name="allowedMentions">The allowed mentions for the followup</param>
        /// <param name="options">The request options for the followup</param>
        public MultiPageEmbed(SocketInteraction interaction, Func<int, EmbedBuilder> getPageFunction, string? text = null, bool isTts = false, 
            bool ephemeral = false, AllowedMentions? allowedMentions = null, RequestOptions? options = null)
        {
            if (!interaction.HasResponded)
            {
                interaction.DeferAsync(ephemeral: true).GetAwaiter().GetResult();
            }

            ComponentBuilder c = GetButtons(true, false);

            _response = interaction.FollowupAsync(embed: getPageFunction(0).Build(), text: text, isTTS: isTts, ephemeral: ephemeral, allowedMentions: allowedMentions, 
                components: c.Build(), options: options, embeds: null).GetAwaiter().GetResult();
            _pages = null;
            _customGetPage = getPageFunction;
            PageNumber = 0;
        }
    }
}