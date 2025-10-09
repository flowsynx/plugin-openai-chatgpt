## FlowSynx OpenAI ChatGPT Plugin

The OpenAI ChatGPT Plugin is a built-in, plug-and-play integration for the FlowSynx automation engine. It enables generating AI responses from OpenAI ChatGPT models within workflows, with no custom coding required.

This plugin is automatically installed by the FlowSynx engine when selected in the workflow builder. It is not intended for standalone developer usage outside the FlowSynx platform.

---

## Purpose

The OpenAI ChatGPT Plugin allows FlowSynx users to:

- Generate conversational responses from a user-provided prompt.
- Summarize content and extract key points.
- Draft emails, messages, or explanations.
- Transform or rephrase text (e.g., simplify, formalize, translate).
- Support decision-making with natural language outputs.

It integrates seamlessly into FlowSynx no-code/low-code workflows for AI-assisted text generation and reasoning tasks.

---

## Input Parameters

The plugin accepts the following parameters:

- `Prompt` (string): Required. The non-empty text prompt sent to ChatGPT.

Example input:

```json
{
  "Prompt": "Summarize the benefits of typed functional programming in one paragraph."
}
```

Notes:
- If `Prompt` is null, empty, or whitespace, execution fails with an argument error.

---

## Specifications

Configure access and model selection via plugin specifications:

- `ApiKey` (string): Required. OpenAI API key used for the Authorization header.
- `ApiUrl` (string): Optional. OpenAI endpoint to call. Default: "https://api.openai.com/v1/chat/completions".
- `Model` (string): Optional. Target model identifier. Default: "gpt-4o-mini".

Example specifications:

```json
{
  "ApiKey": "<your-openai-api-key>",
  "ApiUrl": "https://api.openai.com/v1/chat/completions",
  "Model": "gpt-4o-mini"
}
```

---

## Output

Returns a FlowSynx context payload containing:

- `Format`: "AI"
- `Content`: The assistant reply text (empty string if none)

A unique identifier is generated for each execution.

---

## Example Use Case in FlowSynx

1. Add the OpenAI ChatGPT plugin to your FlowSynx workflow.
2. Provide the `Prompt` in the node input.
3. Configure specifications (set `ApiKey`, optionally override `Model` and `ApiUrl`).
4. Use the plugin output downstream in your workflow for further processing or routing.

---

## Debugging Tips

- Ensure `ApiKey` is set and valid; initialization fails without it.
- Provide a non-empty `Prompt`.
- Verify `Model` exists for your OpenAI account/region.
- Check network access to `ApiUrl` and inspect HTTP status codes.

---

## Security Notes

- No data is persisted unless explicitly configured by your workflow.
- All requests execute within the FlowSynx runtime.
- Keep `ApiKey` secret; manage it via FlowSynx specifications, not hard-coded values.

---

## License

© FlowSynx. All rights reserved.