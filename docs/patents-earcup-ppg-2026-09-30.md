# Patents on heart-rate / SpO2 sensing inside a headset ear cushion

**Compiled 30 September 2026 by KILO at Stefan's request.** A landscape scan of who holds patent rights over
putting an optical pulse sensor in or around the ear cup of a headset, with the aviation cases called out.

> **This is not legal advice and it is not a freedom-to-operate opinion.** I am not a patent attorney. I read
> published patent records; I did **not** perform claim-by-claim infringement analysis, check the file
> histories, examine whether claims were narrowed during prosecution, verify maintenance-fee status, or search
> outside the US. Every item below needs a professional check before any product decision rests on it.
> Dates and assignees are `[fetched]` from Google Patents on 30 Sep 2026; readings of claim scope are
> `[assumed]` unless the claim text is quoted.

---

## 1 · The bottom line

**All three of the ear placements we measured are claimed by somebody**, and two of the three holders are
aviation-headset companies — direct competitors, not distant patent trolls.

| Placement | Our measurement (21 Sep) | Who has claims there |
|---|---|---|
| **Earlobe / just below the ear** | **best** — 4.35 % pulse | **Seabeck Holdings** — a replacement ear cushion with a pulse oximeter reading through the lobe |
| **In front of the tragus** | second — good, consistent | **Lightspeed Aviation** — a circumaural headset with a biometric sensor positioned 5–50 mm forward of the tragus |
| **Behind the ear (mastoid)** | worst — 0.5 %, unusable | **Spotlight Labs (SPYDR)** — headgear sensor contacting the head just behind the ear over bone |

That does not mean the H1 is blocked. Patent claims are narrow and specific: each of these covers a *particular
mechanism or geometry*, not the whole idea of sensing at the ear. But it does mean **this needs a proper
freedom-to-operate search before the cushion design is committed**, not after.

---

## 2 · The patents that matter most

| Patent | Holder | Priority | Expires | What it is about |
|---|---|---|---|---|
| [US10701470B2](https://patents.google.com/patent/US10701470B2/en) | **Lightspeed Aviation** | 7 Sep 2017 | 16 Oct 2037 | Circumaural headset, adjustable biometric sensor mount, sensor forward of the tragus |
| [US12064247B2](https://patents.google.com/patent/US12064247B2/en) | **Spotlight Labs** (SPYDR) | 20 Apr 2017 | 17 May 2040 | Headgear sensor behind the ear over bone, blood-oxygen by near-infrared |
| [US11677428B2](https://patents.google.com/patent/US11677428B2/en) + family | **Seabeck Holdings** | 22 Apr 2016 | 24 Apr 2037 | Aviation headset with physiological sensors; family includes a **replacement cushion** with an earlobe pulse oximeter |
| Valencell portfolio (e.g. [US8157730](https://patents.google.com/patent/US8157730)) | **Valencell** | 2006–2008 onward | early ones from **2026–2029** | Foundational ear-worn PPG: light guiding, motion-noise removal. Licensed, and litigated |
| [US20220087609A1](https://patents.google.com/patent/US20220087609A1/en) | **Bose** | 6 Jan 2020 | ~2041 if granted | In-ear PPG in the ear canal — less relevant to a circumaural cushion |

### Lightspeed Aviation — US10701470 `[fetched]`

Claim 1 covers a circumaural headset whose biometric sensor sits on an **adjustable mount**: an arcuate rack
inside the earcup, a transverse rack sliding against it, and a sensor base sliding along that. Dependent claims
add a spring-biased rotatable cap with an off-centre aperture. The specification places the sensor **"forward
of a tragus", in a target area "between 5 mm–50 mm in front of the tip of the tragus"**, sitting in the
earcup's opening, "either overlapping the cushion side or integrated within the cushion itself".

**Reading it `[assumed]`:** the *inventive step* claimed is the adjustable rack-and-pinion mount, not the mere
presence of a sensor near the tragus. A **fixed** sensor moulded into a cushion at that site may well fall
outside these claims. That distinction is exactly what a patent attorney must confirm — it is the difference
between a design change and a licence.

### Spotlight Labs — US12064247 `[fetched]`

Claim 1 covers headgear with a physiological sensor positioned to contact the head **"just behind the ear at a
local skeletal eminence, with substantially no muscle tissue between skin and bone"**, detecting blood
oxygenation, with the sensor "disposed on or embedded in a portion of the earcup ... contacting a user's skin
over a major branch of the external carotid artery".

This is the mastoid placement, and it is the one **the fleet's own research recommended** on the strength of
SPYDR's results. Two consequences: it is claimed until 2040, **and** our own measurements found that site
optically the worst of four. Avoiding it costs us nothing and removes the most explicit claim overlap.

SPYDR is also the most developed competitor: US Air Force safe-to-fly approval, an accuracy figure of 2.61 %
RMS over 70–100 % oxygen saturation, and a product that turns a **Bose A30 aviation headset** into a monitor
`[fetched: spotlighttechnologies.com, aero-news.net, afmc.af.mil]`. They have done the desaturation study that
we have parked.

### Seabeck Holdings — US11677428 and family `[fetched]`

Priority 2016, four granted US patents (US10419053, US10700725, US11677428) and **a continuation still
pending** (US20230378990A1) — meaning they can still pursue new claim wording against what the market does
next. The specification describes a **replacement cushion for an aviation headset** containing "a pulse
oximeter having a red LED and an infrared LED ... and at least one light sensor tuned to red and infrared
wavelengths", with an **earlobe receptacle** through which light is transmitted.

**Caution:** I could not read the granted claim 1 text directly; whether the granted claims are limited to the
earlobe receptacle or reach a general "physiological sensor in an aviation headset cushion" is **unresolved and
important**. It is the single biggest open question in this scan, because our best measured site is the lobe.

### Valencell — the licensing question `[fetched]`

Valencell holds the foundational ear-worn PPG portfolio and has both **licensed** it (Samsung, Sony, Jabra, LG)
and **enforced** it (Apple — settled 2019; Fitbit; Bragi — dismissed with prejudice). Their early filings
(2006–2008) start expiring around **2026–2029**, so some fundamentals are falling into the public domain now.
For a company our size, Valencell is more plausibly a licensing counterpart than an adversary.

### Useful prior art

[US5372134](https://patents.google.com/patent/US5372134) — "Aviation hypoxia monitor", filed 1993, **long
expired**. Anything it discloses is free to use and can be cited against later claims. Worth having an attorney
read it: early aviation pulse-oximetry disclosures are a defensive asset.

---

## 3 · What this means for the H1

**Three realistic routes, in increasing cost:**

1. **Design around.** Each claim above is tied to something specific: an adjustable rack mount (Lightspeed), a
   behind-the-ear skeletal site (Spotlight), an earlobe receptacle with transmissive optics (Seabeck). A
   **fixed reflectance sensor moulded into the cushion**, at the lobe or in front of the tragus, may sit
   outside all three — but "may" has to become "does", in writing, from an attorney.
2. **License.** Valencell licenses openly. Spotlight and Lightspeed are competitors and unlikely to.
3. **Buy the module rather than build it.** A bought-in, already-licensed sensor module shifts the patent
   exposure onto the supplier. This also answers the calibration problem we parked, since such modules come
   with validated accuracy.

**The thing not to do** is finalise cushion geometry around a placement, then discover it reads onto a
competitor's claim. Ear placement is cheap to change now and expensive to change after tooling.

---

## 4 · Recommended next steps

1. **Get the granted claims of the Seabeck family read by a patent attorney** — specifically whether they
   reach beyond an earlobe receptacle. That one answer most affects our best measured placement.
2. **Commission a freedom-to-operate search** scoped to: optical PPG/SpO2 sensing in or on a circumaural
   headset ear cushion, in the jurisdictions you will actually sell in (US and EP at minimum; South Africa
   too if manufacturing here). Typical FTO work is a few thousand dollars and a few weeks `[assumed — get
   quotes]`.
3. **Document our own development record.** Everything measured is dated, committed and traceable in this
   repo. That record is evidence of independent development and a base for our own filing if the H1's sensing
   approach turns out to be novel — which our ear-site measurements suggest it might, since we found the
   opposite of what the published competitor art recommends.
4. **Do not publish our placement findings** until (3) is decided — publication can bar your own later filing.
   Note the phone app is already public, though it contains no H1 placement data.

---

## 5 · What this scan did not cover

- **No claim-by-claim analysis** and no legal opinion. Claim scope stated above is my reading, not counsel's.
- **US only.** No European, Chinese, or South African search; equivalents almost certainly exist.
- **Design patents and trade dress** were not searched (Spotlight and others hold design registrations on
  earcups — e.g. USD878327S1).
- **Maintenance status not verified.** A granted patent can lapse for unpaid fees; that changes everything and
  is quick for an attorney to check.
- **Nothing here says whether any claim is valid.** Claims fall regularly on prior art — and there is
  substantial ear-oximetry prior art from the 1990s.

---

## Sources

Patent records via Google Patents (fetched 30 Sep 2026): [US10701470B2](https://patents.google.com/patent/US10701470B2/en) ·
[US12064247B2](https://patents.google.com/patent/US12064247B2/en) · [US11677428B2](https://patents.google.com/patent/US11677428B2/en) ·
[US20170324437A1](https://patents.google.com/patent/US20170324437A1/en) · [US8157730](https://patents.google.com/patent/US8157730) ·
[US20220087609A1](https://patents.google.com/patent/US20220087609A1/en) · [US5372134](https://patents.google.com/patent/US5372134) ·
[US8548547B2](https://patents.google.com/patent/US8548547B2/en) (cockpit-panel hypoxia system, not a headset).
Valencell litigation history: [MobiHealthNews](https://www.mobihealthnews.com/news/valencell-sues-apple-fitbit-over-heart-rate-patents),
[AppleInsider](https://appleinsider.com/articles/19/01/22/apple-settles-apple-watch-heart-rate-sensor-lawsuit-with-biometrics-firm-valencell),
[PatSnap on Valencell v. Bragi](https://www.patsnap.com/resources/blog/litigation/valencell-v-bragi-wearable-biometric-sensor-patent-dispute-patsnap/).
SPYDR: [Spotlight Technologies](https://www.spotlighttechnologies.com/technologies),
[AFMC safe-to-fly](https://www.afmc.af.mil/News/Article-Display/Article/4005921/aeromedical-branch-makes-safe-to-fly-recommendation-for-spydr-device/).
