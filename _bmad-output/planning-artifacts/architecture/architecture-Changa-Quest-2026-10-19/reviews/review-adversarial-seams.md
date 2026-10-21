# Adversarial Review — Architecture Seams

**Scope:** Updated architecture spine, `ARCHITECTURE-SPINE.md` (especially AD-3 through AD-6, the Structural Seed, and Deferred). Review focused on whether independently implemented downstream units could comply with the architecture decisions yet still diverge incompatibly at shared data shapes, mutation ownership, concurrency/state transitions, or client/API boundaries. The PRD was consulted only to confirm the intended selection-assignment flow and its outcomes.

**Method:** Read the spine’s explicit rules and deferred decisions, then tested concrete pairs of downstream units against them. Findings below are limited to gaps where the stated constraints still permit materially incompatible behavior. This is a document-level review; no implementation or tests were inspected.

## Findings

### Blocking — The API and web client have no shared wire-contract definition

**Evidence:** The Structural Seed says the Blazor client consumes the REST API (`ARCHITECTURE-SPINE.md`, lines 123–125, 134). However, the exact DTOs and endpoints are deferred (line 161), and the stack leaves client asset delivery and hosting to be resolved later (line 163). AD-6 standardizes the error media type and some error behavior (lines 86–90), but does not define successful response shapes or a contract-validation mechanism.

**Divergence:** The API and client can both follow the REST boundary and AD-6 while independently choosing incompatible field names, nullability, enum encodings, or representations of a result. For example, one unit could serialize an assignment state as a string while the other expects an integer.

**Impact:** The client may fail to deserialize or may misinterpret valid server responses, even though both units comply with the spine.

**Recommendation:** Define a versioned API contract or equivalent canonical schema covering request/response DTOs, nullability, enum representation, and error responses. Require both API and client to validate against that contract.

### Blocking — Selection assignment has no specified atomic unit across assignment and application outcomes

**Evidence:** AD-3 assigns lifecycle mutation ownership to `ChangaManager` and requires other Managers to request its transitions (`ARCHITECTURE-SPINE.md`, line 72). AD-4 requires incompatible lifecycle transitions on one changa to be validated against persisted state and confirmed atomically (line 78). The PRD says selecting an application assigns that applicant and marks the other applications as not selected (PRD, FR-8, “Consecuencias comprobables”).

**Divergence:** The spine does not say whether assignment and the losing applications’ state changes form one atomic transition. A downstream unit could commit the changa assignment first and update applications separately; another could treat the entire operation as one transaction. A failure between writes can leave the changa assigned while applications remain open, or close applications without a corresponding assignment.

**Impact:** Persistent state can contradict the selection outcome required by FR-8, and later requests may produce different results depending on which partial state was committed.

**Recommendation:** Define the full postconditions for selecting an application—including the selected application, changa assignment, and non-selected applications—and specify that they commit or roll back together under the lifecycle transition’s coordinating owner.

### Non-Blocking — Retry behavior after a committed assignment is unspecified

**Evidence:** AD-4 covers duplicate assignment prevention under concurrency and retries, and says BusinessLogic communicates the business result (`ARCHITECTURE-SPINE.md`, line 78). AD-6 requires stable error codes when clients need to act on an error type, but does not specify assignment retry semantics or a particular result for a repeat request (line 90).

**Divergence:** If the server commits an assignment but its response is lost, the client may retry. The API could return the original successful assignment, or return a conflict because the changa is no longer available; both choices can satisfy “asignación no puede duplicarse.” A client built for one behavior may handle the other as a failure or leave the user unsure whether the assignment succeeded.

**Impact:** Retried requests can create inconsistent user-visible outcomes and unnecessary recovery steps, even if storage prevents duplicate assignments.

**Recommendation:** Specify whether assignment requests are idempotent or how a retry by the same actor is resolved, and define the stable response/error contract for that case.

## Summary

The spine establishes important layer and lifecycle ownership rules, but it leaves three concrete interoperability seams: the client/server contract, the atomic scope of selection assignment, and assignment retry semantics.

**Requested report path:** `C:\Users\emanu\OneDrive\Desktop\Changa-Quest\_bmad-output\planning-artifacts\architecture\architecture-Changa-Quest-2026-10-19\reviews\review-adversarial-seams.md`  
**Original review status:** The reviewer could not write the file; this preserved report is based on its complete returned review.
