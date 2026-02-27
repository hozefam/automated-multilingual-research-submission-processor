# Capstone Project: Automated Multilingual Research Submission Processor

## 1. Project Overview

**Project Name:** Automated Multilingual Research Submission Processor

**Project Description:**
A multi-agent AI system that automatically ingests research paper submissions (emails + attachments), extracts structured metadata and key content (title, authors, affiliations, abstract, keywords, figures), validates formatting and compliance (page limits, references), generates a validation summary, and provides a RAG-powered conversational Q&A on stored submissions.

The system must support multiple languages, including a Human-In-The-Loop (HITL) review mechanism for flagged/low-confidence items, and learn from human corrections to improve future extraction and validation.

**Business Context:**
Research submissions are often received in multiple formats (PDF, Word, Scanned images) and languages. Manual review for quality, plagiarism, formatting compliance and summary generation is time consuming and error prone. This project aims to automate document extraction, validation, summarization and review assistance using a multi-agent powered system with human-in-the-loop oversight.

---

## 2. Project Requirements

Design and implement an end-to-end Agentic AI-powered solution that:

- **Monitors an email inbox** for incoming research paper submissions

  > _(For this step only design/approach is expected — not actual implementation)_

- **Detects the language** of each submission using a language detection model

- **Uses OCR** for image-based or scanned submissions

- **Translates extracted data to English** — stores both the original and English translations in memory for reference

- **Validates each submission** against the following rules:
  - Minimum page count: **8**
  - Maximum page count: **25**
  - Required sections: **Title, Abstract, Keywords, Authors, References**
  - Submission must undergo a **plagiarism check**

- **Generates a summary** highlighting:
  - Key findings
  - Major validation issues
  - Missing sections

- **Checks for toxicity and illicit content** and sends for human review when detected

- **Implements a RAG pipeline** for retrieval and Q&A

- **Provides admin override/correction** of AI-generated validation findings

- **Generates a validation summary report** for each submission

- **Logs all actions** taken by users and administrators

---

## 3. Agent Roles

| #   | Agent                          | Responsibility                                                                                                                       |
| --- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | **Ingestion Agent**            | Continuously monitors the mailbox for emails with research paper attachments. _(Reads from file system rather than a live mailbox)_  |
| 2   | **Pre-process Agent**          | Prepares data for extraction: validates file type, detects language, and runs OCR for scanned documents                              |
| 3   | **Translation Agent**          | Translates extracted data from different languages into English; stores both original and translated text                            |
| 4   | **Extraction Agent**           | Extracts structured fields from submissions (title, authors, affiliations, abstract, keywords, figures)                              |
| 5   | **Validation Agent**           | Validates business rules (page count, required sections) and performs semantic checks                                                |
| 6   | **Content Safety Agent**       | Checks for toxicity and illicit content; flags for human review when violations are detected                                         |
| 7   | **Plagiarism Detection Agent** | Cross-references submissions against academic databases; flags if similarity > 25%                                                   |
| 8   | **RAG Agent**                  | Generates embeddings and maintains the vector store for retrieval, augmentation and generation                                       |
| 9   | **Summary Agent**              | Produces a human-readable summary report for reviewers in **≤ 250 words**                                                            |
| 10  | **Q&A Agent**                  | Handles conversational queries about submissions; supports multilingual input and maintains chat history                             |
| 11  | **Human Feedback Agent**       | Presents flagged items to the admin for HITL review, accepts corrections, and stores them. Flags automatically when confidence < 25% |

---

## 4. Solution Architecture

The system is built on a **modular agentic architecture** using the **Microsoft Agent Framework**.

### RAG-Based Q&A System

The RAG pipeline enables contextual queries via dedicated agents for:

- **Indexing** — chunking and embedding documents
- **Retrieval** — vector similarity search
- **Augmentation** — context injection into prompts
- **Generation** — LLM response generation
- **Reflection** — response quality evaluation

### Cross-Cutting Concerns

- **Human-in-the-loop (HITL) feedback** — admin review and correction of low-confidence results
- **Audit trails** — all system and user actions are logged for reliability and transparency
- **Prompt templates** — all agent prompts use structured SK prompt templates

### Semantic Kernel Capabilities (Required)

The following SK capabilities **must** be used across one or more agents:

| Capability               | Usage                                                                             |
| ------------------------ | --------------------------------------------------------------------------------- |
| **Semantic Functions**   | Prompt-template-based functions for extraction, summarization, Q&A                |
| **Native Functions**     | C# method-based functions for OCR, file parsing, validation rules                 |
| **Memory**               | Vector store for RAG indexing and retrieval; correction storage for HITL learning |
| **Plugins**              | Agent-specific plugin classes grouping related Native + Semantic Functions        |
| **Filters with Logging** | `IFunctionInvocationFilter` + `IPromptRenderFilter` for auditing all SK calls     |
| **Agent Framework**      | `ChatCompletionAgent` per agent role with system prompts and plugin bindings      |
| **Process Framework**    | `KernelProcess` + `KernelProcessStep` to replace the sequential orchestrator      |
