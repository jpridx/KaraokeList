# AI Agent Ideas for the Karaoke App

## Goal

Build an AI agent as a learning project that teaches modern agent
architecture while leveraging the existing KaraokeList application.

Rather than creating a toy chatbot, use the karaoke domain as a safe
environment to learn concepts that will later transfer to enterprise
applications (ERP, CRM, reporting, etc.).

------------------------------------------------------------------------

# What an AI Agent Is

An agent is more than an LLM.

It combines:

-   **Reasoning** (the language model)
-   **Memory / State**
-   **Tools** (REST APIs, databases, web searches, etc.)
-   **Planning**

For a first project, keep planning simple and focus on tool use.

------------------------------------------------------------------------

# Suggested Learning Projects

## 1. Karaoke Assistant (Recommended)

A conversational assistant that answers questions such as:

> I'm going to karaoke tonight. I sing Journey and Survivor. What else
> should I sing?

The agent should:

-   Search the song database
-   Consider artists
-   Consider vocal style
-   Recommend songs
-   Explain why they were chosen

### Learning Concepts

-   Tool calling
-   Structured outputs
-   Prompt engineering
-   Keeping SQL behind the API

------------------------------------------------------------------------

## 2. Song Discovery Agent

Example:

> Find songs released after 2005 that sound like "Burning for You."

Possible tools:

-   Karaoke database
-   MusicBrainz
-   Spotify (future)

The agent coordinates multiple tools to build a recommendation.

------------------------------------------------------------------------

## 3. Playlist Builder

Example objective:

> Build a 20-song karaoke rotation.

Constraints:

-   No duplicate artists
-   Stay within vocal range
-   Alternate fast/slow songs
-   Avoid songs recently performed

Introduces planning instead of simple question answering.

------------------------------------------------------------------------

## 4. Metadata Curator

Automatically improve song metadata.

Examples:

-   Missing genre
-   Missing release year
-   Missing songwriter
-   Incorrect capitalization

Workflow:

1.  Search external databases
2.  Compare results
3.  Assign confidence
4.  Automatically update if confidence is high
5.  Queue uncertain items for review

------------------------------------------------------------------------

## 5. Duplicate Detector

Find probable duplicate songs such as:

-   Separate Ways
-   Separate Ways (Worlds Apart)
-   Separate Ways (Remastered)

Group likely duplicates and ask for confirmation.

------------------------------------------------------------------------

## 6. Data Cleanup Agent

Improve overall database quality.

Tasks:

-   Normalize artist names
-   Fix capitalization
-   Detect typos
-   Merge genres
-   Find missing release dates
-   Flag suspicious durations

------------------------------------------------------------------------

## 7. Natural Language Search

Translate conversational requests into API calls.

Examples:

-   Female country duets from the 90s
-   Songs that start quietly and build
-   One-hit wonders from the 80s

------------------------------------------------------------------------

# Technology Stack

Since the application already uses .NET:

-   ASP.NET Core
-   Azure OpenAI
-   Microsoft.Extensions.AI
-   Semantic Kernel
-   Existing REST API

The LLM should **never** access SQL directly.

Instead it should call tools such as:

-   SearchSongs()
-   GetSong()
-   SearchArtists()
-   SearchGenres()
-   RecommendByArtist()

------------------------------------------------------------------------

# Recommended Solution Structure

    KaraokeList.sln

        KaraokeList.Client      (Blazor WASM)
        KaraokeList.Api         (Existing REST API)
        KaraokeList.Shared

        KaraokeList.Agent       (New AI Agent)

The agent should consume the existing API just like any other client.

Benefits:

-   Business rules remain in one place.
-   The API is exercised exactly as production clients use it.
-   The agent can later be deployed independently.
-   SQL remains hidden.

------------------------------------------------------------------------

# Overall Architecture

    Blazor WASM
          |
          v
    Agent API
          |
          +----------------+
          |                |
          v                v
    Azure OpenAI     Karaoke REST API
                          |
                          v
                     SQL Database

The agent knows nothing about SQL.

------------------------------------------------------------------------

# Playground Project

Initially, consider creating:

    KaraokeList.Agent.Playground

or

    KaraokeList.AgentLab

This sandbox allows experimentation with:

-   Prompts
-   Tool definitions
-   Models
-   Memory
-   Retry strategies

without affecting the production application.

------------------------------------------------------------------------

# Suggested Abstractions

Rather than calling the LLM directly everywhere, create simple
abstractions:

    IAgent

    KaraokeAgent

    ITool

    SearchSongsTool

    GetSongTool

    RecommendSongsTool

This mirrors the architecture used by modern agent frameworks and makes
the project easier to extend.

------------------------------------------------------------------------

# Learning Roadmap

1.  Single tool call
2.  Multiple tool calls
3.  Reasoning over results
4.  Planning
5.  Memory
6.  Autonomous tasks

Each stage introduces one new concept while building toward a genuinely
useful assistant.

------------------------------------------------------------------------

# Long-Term Vision

The Karaoke Concierge should eventually:

-   Search the catalog
-   Recommend songs
-   Build balanced set lists
-   Explain recommendations
-   Remember user preferences
-   Perform maintenance tasks such as metadata cleanup

Although the project centers on karaoke, the same architectural patterns
transfer directly to future enterprise AI agents that work with ERP,
CRM, reporting, and other business systems.
