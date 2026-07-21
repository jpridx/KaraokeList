# 06 — Using an LLM as a Service

## Overview

KaraokeList uses an LLM as an **optional assistant**, not as a source of truth. Genre suggestion calls OpenAI Chat Completions (`gpt-4o-mini`) **from the API** so the API key never ships to the browser. The model must pick from the app’s existing genre catalog; the UI always allows a manual override.

| Piece | Role |
|-------|------|
| `AiGenreService` | Calls OpenAI, parses JSON, maps name → catalog id |
| `AiController` | Authorized `POST api/ai/suggest-genre` |
| `AddGenreField.razor` | Explicit “Suggest genre” button + fallback message |
| `Ai:OpenAiKey` | Server-side configuration / secret |

Design principles and future candidates: `docs/ai-integration.md`.

## Major aspects

1. **Server owns the key** — WASM never sees `Ai:OpenAiKey`.
2. **Constrained output** — prompt includes the live genre list; JSON object format enforced.
3. **Catalog reconciliation** — model string must match a real `GenreName` (case-insensitive).
4. **Degrade gracefully** — missing key or unmatched suggestion → empty response, UI continues manually.
5. **Explicit user action** — suggestion is opt-in, not automatic on every keystroke.
6. **Cost & latency awareness** — small model, narrow task, no unbounded chat UI.
7. **Authorize the endpoint** — AI is behind the same JWT gate as other app APIs.

## Code samples

### Sample 1 — Call OpenAI only when configured; constrain to catalog genres

```21:54:KaraokeList.Api/Services/AiGenreService.cs
    public async Task<GenreSuggestionResponse> SuggestGenreAsync(string title, string artist)
    {
        var apiKey = configuration["Ai:OpenAiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GenreSuggestionResponse();
        }

        var genres = await genreService.GetGenresAsync();
        // ...
        var client = new ChatClient(model: "gpt-4o-mini", apiKey: apiKey);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(
                "You are a music genre classifier. " +
                "When given a song title and artist, respond with a JSON object containing a single key \"genre\" " +
                "whose value is exactly one genre name from the provided list. " +
                // ...
            ),
            ChatMessage.CreateUserMessage(
                $"Song: \"{title}\" | Artist: \"{artist}\" | Genres: {genreList}")
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };
```

### Sample 2 — Map model output back to a catalog id

```72:77:KaraokeList.Api/Services/AiGenreService.cs
        var match = genres.FirstOrDefault(g =>
            string.Equals(g.GenreName, suggested.Trim(), StringComparison.OrdinalIgnoreCase));

        return match is not null
            ? new GenreSuggestionResponse { GenreId = match.Id, GenreName = match.GenreName }
            : new GenreSuggestionResponse();
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api/Controllers/AiController.cs` | full | Thin authorized endpoint wrapping the service |
| `KaraokeList.Web/Components/AddGenreField.razor` | ~136–161 | UI suggest flow and “pick manually” fallback |
| `docs/ai-integration.md` | full | Architecture principles, prompt shapes, cost/control guidelines |

## Exercises

1. **Multiple choice.** Where should the OpenAI API key live?
   - A) `wwwroot/appsettings.json` in the WASM project
   - B) Server-side API configuration / secrets
   - C) Hard-coded in `AddGenreField.razor`
   - D) Inside the JWT

2. **Fill in the blank.** The chat model used for genre suggestion is ________.

3. **Multiple choice.** If `Ai:OpenAiKey` is missing, `SuggestGenreAsync` should:
   - A) Throw and crash the API
   - B) Return an empty suggestion so the UI can continue manually
   - C) Call MusicBrainz instead
   - D) Invent a new genre row

4. **Fill in the blank.** The response is constrained with `ChatResponseFormat.Create________Format()`.

5. **Multiple choice.** Why include the genre list in the prompt?
   - A) To train a new model
   - B) So the model picks from known catalog values the app can resolve to ids
   - C) OpenAI requires SQL schemas
   - D) To bypass JWT auth

6. **Fill in the blank.** Genre name matching uses ________IgnoreCase comparison.

7. **Multiple choice.** AI suggestion in the UI is triggered by:
   - A) Every keypress automatically
   - B) An explicit user action (Suggest genre)
   - C) Service worker install
   - D) Dependabot

8. **Fill in the blank.** Shared request/response types for AI live in ________ (file or type area under Shared).

9. **Multiple choice.** Calling the LLM from the API (not WASM) primarily protects:
   - A) Syncfusion licenses
   - B) The API key and server-side control of prompts/cost
   - C) CORS headers
   - D) SQL indexes

10. **Fill in the blank.** When no genre matches, the service returns a ________ `GenreSuggestionResponse`.

## Answer key

1. B  
2. `gpt-4o-mini`  
3. B  
4. `JsonObject`  
5. B  
6. `Ordinal` (`StringComparison.OrdinalIgnoreCase`)  
7. B  
8. `AiDtos.cs` (or `KaraokeList.Shared`)  
9. B  
10. empty (default / null fields)  
