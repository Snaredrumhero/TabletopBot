using System.Diagnostics.CodeAnalysis;

namespace TableTopBot
{
    public class MultiPageEmbed
    {
        private RestFollowupMessage? _response;
        private readonly EmbedBuilder[]? _pages;
        
        string text;
        bool isTts;
        bool ephemeral;
        AllowedMentions allowedMentions;
        RequestOptions options;
        string nextButton;
        string backButton;

        public int PageNumber
        {
            get;
            private set;
        }

        public bool IsLastPage => !TryGetPage(PageNumber + 1, out _);

        public async Task<bool> UpdatePage(int pageNumber)
        {
            EmbedBuilder? builder = GetPageOrDefault(pageNumber);

            if (builder is null)
            {
                return false;
            }

            PageNumber = pageNumber;
            
            await _response!.ModifyAsync(z =>
            {
                z.Embeds = new Optional<Embed[]>(new[] { builder.WithCurrentTimestamp().Build() });
                z.Components = new Optional<MessageComponent>(GetButtons(PageNumber == 0, IsLastPage, backButton, nextButton).Build());
            });
            
            return true;
        }

        public async Task<bool> NextPage()
        {
            return await UpdatePage(PageNumber + 1);
        }

        public async Task<bool> PreviousPage()
        {
            return await UpdatePage(PageNumber - 1);
        }

        public EmbedBuilder GetPage(int pageNumber)
        {
            if (_customGetPage is not null)
            {
                return _customGetPage(pageNumber);
            }

            if (pageNumber < 0 || pageNumber >= _pages!.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            return _pages[pageNumber];
        }

        public EmbedBuilder? GetPageOrDefault(int pageNumber, EmbedBuilder? defaultBuilder = null)
        {
            return TryGetPage(pageNumber, out EmbedBuilder? builder) ? builder : defaultBuilder;
        }

        public bool TryGetPage(int pageNumber, [MaybeNullWhen(false)] out EmbedBuilder builder)
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

        private static ComponentBuilder GetButtons(bool isBackDisabled, bool isNextDisabled, string back, string next)
        {
            ComponentBuilder c = new();
            c.WithButton(ButtonBuilder.CreatePrimaryButton("Previous Page", back).WithDisabled(isBackDisabled));
            c.WithButton(ButtonBuilder.CreatePrimaryButton("Next Page", next).WithDisabled(isNextDisabled));
            return c;
        }
        
        public async Task<bool> StartPage(SocketInteraction interaction)
        {
            if (!interaction.HasResponded)
            {
                interaction.DeferAsync(ephemeral: true).GetAwaiter().GetResult();
            } 
            
            
            //bool.equal is used to check if there is more than 1 page in _pages            
            ComponentBuilder c = GetButtons(true, bool.Equals(_pages!.Count(), 1), backButton, nextButton);

            _response = await interaction.FollowupAsync(embed: _pages!.First().WithCurrentTimestamp().Build(), text: text, isTTS: isTts, ephemeral: ephemeral, allowedMentions: allowedMentions, 
                components: c.Build(), options: options, embeds: null);
            return true;
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
        /// <param name="BackButton">The id of the back button</param>
        /// <param name="NextButton">The id of the next button</param>
        public MultiPageEmbed(IEnumerable<EmbedBuilder> pages, string? Text = null, bool IsTts = false,
            bool Ephemeral = false, AllowedMentions? AllowedMentions = null, RequestOptions? Options = null, 
            string BackButton = "back-button", string NextButton = "next-button")
        {
            _pages = pages.ToArray();
            if (_pages.Length== 0)
            {
                throw new InvalidOperationException("Pages must contain at least one item");
            }
            for(int i = 0; i < _pages.Count(); ++i)
            {
                _pages[i].WithFooter($"Page {i+1}/{_pages.Count()}");
            }
            
            _customGetPage = null;
            text = Text!;
            isTts = IsTts;
            ephemeral = Ephemeral;
            allowedMentions = AllowedMentions!;
            options = Options!;
            PageNumber = 0;
            backButton = BackButton;
            nextButton = NextButton;
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
        public MultiPageEmbed(Func<int, EmbedBuilder> getPageFunction, string? Text = null, bool IsTts = false,
            bool Ephemeral = false, AllowedMentions? AllowedMentions = null, RequestOptions? Options = null,
            string BackButton = "back-button", string NextButton = "next-button")
        {

            _pages = null;
            _customGetPage = getPageFunction;
            PageNumber = 0;
            
            text = Text!;
            isTts = IsTts;
            ephemeral = Ephemeral;
            allowedMentions = AllowedMentions!;
            options = Options!;
            nextButton = NextButton;
            backButton = BackButton;
        }
    }
}