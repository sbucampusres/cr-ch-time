# Stony Brook University Style Guide for ASP.NET Core Applications

This style guide is based on the official Stony Brook University brand guidelines and is intended for use in ASP.NET Core application development. Following these guidelines will ensure consistency with SBU's brand identity across all digital applications.

## Table of Contents

1. [Typography](#typography)
   - [Print Usage](#print-usage)
   - [Web Usage](#web-usage)
   - [Font Recommendations for ASP.NET Core](#font-recommendations-for-aspnet-core)
2. [Colors](#colors)
   - [Primary Colors](#primary-colors)
   - [Secondary Colors](#secondary-colors)
   - [Gradients](#gradients)
   - [Accessibility](#accessibility)
   - [Color Don'ts](#color-donts)
3. [Implementation Guide for ASP.NET Core](#implementation-guide-for-aspnet-core)
   - [CSS Variables](#css-variables)
   - [Tailwind Configuration](#tailwind-configuration)
   - [Bootstrap Customization](#bootstrap-customization)
4. [Component Examples](#component-examples)

---

## Typography

### Print Usage

While not directly applicable to web applications, understanding the print guidelines helps maintain brand consistency across all mediums.

#### Primary Fonts

1. **Alumni Sans** - Used for display, headlines, subheads, and quotes
   - Always in all caps for headlines (72% leading)
   - Sentence case for subheads and quotes (100% leading)
   - Weights: ExtraBold, Bold, SemiBold, Medium, Regular, and Light
   - Bold weight is recommended for "Dare to Be" headlines

2. **Bitter** - Used for body copy, can also be used for headlines, subheads, and quotes when necessary
   - Sentence case (120% leading)
   - All caps for callouts or eyebrows (105% leading)
   - Weights: Extra Bold, Bold, Semi Bold, Medium, Regular, and Light

3. **Barlow Semi Condensed** - Used for body copy when other primary fonts don't provide the best legibility
   - Headlines in all caps (90% leading)
   - Sentence case for body copy (120% leading)
   - Weights: Bold, Semi Bold, Medium, Regular, and Light

#### Secondary Fonts

1. **Barlow Condensed** - Used for signage and projects with a long shelf-life
   - Headlines in all caps (90% leading)
   - Sentence case for body copy (120% leading)
   - Weights: Bold, Semi Bold, Medium, Regular, and Light

2. **Barlow** - Used for body copy when better legibility is needed
   - Headlines in all caps (90% leading)
   - Sentence case for body copy (120% leading)
   - Weights: Bold, Semi Bold, Medium, Regular, and Light

### Web Usage

#### Primary Fonts

1. **Alumni Sans**
   - For display, headlines, subheads and emphasized text
   - All caps display headlines (75% line spacing)
   - Sentence case display headlines (85% line spacing)
   - All caps callouts (90% line spacing)
   - Sentence case callouts (100% line spacing)
   - Body text: At least 30px with 120% line spacing
   - Weights: Extra Bold, SemiBold, Medium, Regular, and Light

2. **Bitter**
   - For headlines, subheads, quotes, and body copy
   - All caps headlines (105% line spacing)
   - Sentence case headlines (115% line spacing)
   - Body text: 16-18px minimum with 165% line spacing
   - Weights: ExtraBold, Bold, SemiBold, Medium, Regular, and Light

3. **Barlow Semi Condensed**
   - For headlines and body copy
   - All caps headlines (100% line spacing)
   - Sentence case headlines (110% line spacing)
   - Body text: 18-20px minimum with 160% line spacing
   - Weights: Bold, SemiBold, Medium, Regular, and Light

#### Secondary Fonts

1. **Barlow Condensed**
   - For headlines, subheads, callouts, and eyebrows
   - All caps headlines (100% line spacing)
   - Sentence case headlines (110% line spacing)
   - Body text: 24px minimum with 130% line spacing
   - Weights: Bold, SemiBold, Medium, Regular, and Light

2. **Barlow**
   - For body copy when more legibility is needed
   - Also for subheads and charts/tables
   - Body text: 18-20px minimum with 150% line spacing
   - Weights: Bold, SemiBold, Medium, Regular, and Light

### Font Recommendations for ASP.NET Core

All recommended fonts are available from Google Fonts, making them easy to integrate into web applications:

```html
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous">
<link href="https://fonts.googleapis.com/css2?family=Alumni+Sans:ital,wght@0,100..900;1,100..900&family=Barlow+Condensed:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Barlow+Semi+Condensed:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Barlow:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Bitter:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&display=swap" rel="stylesheet">
```

---

## Colors

### Primary Colors

Stony Brook Red and Stony Brook Black are the primary brand colors and should be prominently featured in all applications.

| Color | Hex | RGB | CMYK | PMS |
|-------|-----|-----|------|-----|
| **Stony Brook Black** | #000000 | 0/0/0 | 0/0/0/100 | BLACK |
| **Rich Black (for print)** | | | 60/40/40/100 | |
| **Stony Brook Red** | #990000 | 153/0/0 | 5/100/71/22 | 187 |

### Secondary Colors

These colors should be used to support the primary colors. The proportion of reds to non-red secondary colors should be approximately 80:20.

| Color | Hex | RGB | CMYK | PMS |
|-------|-----|-----|------|-----|
| **Stony Brook Dark Red** | #6B000D | 107/0/13 | 7/100/65/55 | 188 |
| **Stony Brook Bright Red** | #D52027 | 213/32/39 | 0/95/80/5 | 185 |
| **Stony Brook Dark Gray** | #4B4B4B | 75/75/75 | 38/28/21/63 | 425 |
| **Stony Brook Medium Gray** | #828282 | 130/130/130 | 18/14/14/38 | 423 |
| **Stony Brook Light Gray** | #BEBEBE | 190/190/190 | 6/5/7/15 | 420 |
| **Stony Brook Navy Blue** | #002244 | 0/34/68 | 100/86/43/48 | 289C |
| **Stony Brook Royal Blue** | #00549A | 0/84/154 | 100/73/0/10 | 7686C |
| **Stony Brook Turquoise** | #1791AD | 23/145/173 | 72/9/9/13 | 7459C |
| **Stony Brook Forest Green** | #104247 | 16/66/71 | 89/22/34/65 | 7476C |
| **Stony Brook Pear** | #F1EA86 | 241/234/134 | 10/0/59/0 | 586C |
| **Stony Brook Celery** | #BCCF9D | 188/207/157 | 24/0/43/0 | 579C |
| **Stony Brook Cream** | #F8F2C5 | 248/242/197 | 1/2/24/0 | 7499C |

### Gradients

Gradients are used to add depth and dimension to designs.

#### Primary Gradients

- Should be made by combining two Stony Brook Red values
- Always linear, never radial
- Angle should be between 70° and 20° (moving up and to the right)

#### Secondary Gradients

- Can be created by pairing secondary Stony Brook colors of the same family
- Should support primary Stony Brook colors and gradients
- Follow the same rules as primary gradients: linear, 70°-20° angle

### Accessibility

For optimal accessibility, ensure proper contrast between text and background colors. Follow WCAG guidelines with a minimum contrast ratio of 4.5:1.

#### Recommended Color Combinations for Text

| Background Color | Text Color | Notes |
|------------------|------------|-------|
| Stony Brook Black | White | |
| Stony Brook Red | White | |
| Stony Brook Dark Red | White | |
| Stony Brook Bright Red | White | |
| Stony Brook Navy Blue | White | |
| Stony Brook Dark Gray | White | |
| Stony Brook Medium Gray | White | |
| Stony Brook Light Gray | Black | |
| Stony Brook Royal Blue | White | |
| Stony Brook Turquoise | White | |
| Stony Brook Forest Green | White | |
| Stony Brook Pear | Black | |
| Stony Brook Celery | Black | |
| Stony Brook Cream | Black | |

Check contrast ratios at: [webaim.org/resources/contrastchecker](https://webaim.org/resources/contrastchecker)

### Color Don'ts

- Do not use gradients that combine different color families
- Do not use gradients that aren't angled up and to the right
- Do not use radial gradients
- Do not let secondary colors overpower primary colors
- Do not use color combinations that fail WCAG Accessibility Guidelines
- Do not use colors that are not part of the SBU Brand Guidelines

---

## Implementation Guide for ASP.NET Core

### CSS Variables

Defining CSS custom properties (variables) for the SBU color palette allows for consistent usage across your application:

```css
:root {
  /* Primary Colors */
  --sbu-black: #000000;
  --sbu-red: #990000;
  
  /* Secondary Colors */
  --sbu-dark-red: #6B000D;
  --sbu-bright-red: #D52027;
  --sbu-dark-gray: #4B4B4B;
  --sbu-medium-gray: #828282;
  --sbu-light-gray: #BEBEBE;
  --sbu-navy-blue: #002244;
  --sbu-royal-blue: #00549A;
  --sbu-turquoise: #1791AD;
  --sbu-forest-green: #104247;
  --sbu-pear: #F1EA86;
  --sbu-celery: #BCCF9D;
  --sbu-cream: #F8F2C5;
  
  /* Typography */
  --font-alumni-sans: 'Alumni Sans', sans-serif;
  --font-bitter: 'Bitter', serif;
  --font-barlow-semi: 'Barlow Semi Condensed', sans-serif;
  --font-barlow-condensed: 'Barlow Condensed', sans-serif;
  --font-barlow: 'Barlow', sans-serif;
}
```

### Tailwind Configuration

If using Tailwind CSS in your ASP.NET Core application, extend the configuration to include SBU colors and fonts:

```javascript
// tailwind.config.js
module.exports = {
  theme: {
    extend: {
      colors: {
        'sbu-black': '#000000',
        'sbu-red': '#990000',
        'sbu-dark-red': '#6B000D',
        'sbu-bright-red': '#D52027',
        'sbu-dark-gray': '#4B4B4B',
        'sbu-medium-gray': '#828282',
        'sbu-light-gray': '#BEBEBE',
        'sbu-navy-blue': '#002244',
        'sbu-royal-blue': '#00549A',
        'sbu-turquoise': '#1791AD',
        'sbu-forest-green': '#104247',
        'sbu-pear': '#F1EA86',
        'sbu-celery': '#BCCF9D',
        'sbu-cream': '#F8F2C5',
      },
      fontFamily: {
        'alumni-sans': ['"Alumni Sans"', 'sans-serif'],
        'bitter': ['Bitter', 'serif'],
        'barlow-semi': ['"Barlow Semi Condensed"', 'sans-serif'],
        'barlow-condensed': ['"Barlow Condensed"', 'sans-serif'],
        'barlow': ['Barlow', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
```

### Bootstrap Customization

If using Bootstrap in your ASP.NET Core application, customize the SCSS variables to match SBU's brand guidelines:

```scss
// Custom.scss

// Override Bootstrap default variables
$primary: #990000; // Stony Brook Red
$secondary: #4B4B4B; // Stony Brook Dark Gray
$success: #BCCF9D; // Stony Brook Celery
$info: #1791AD; // Stony Brook Turquoise
$warning: #F1EA86; // Stony Brook Pear
$danger: #D52027; // Stony Brook Bright Red
$dark: #000000; // Stony Brook Black
$light: #F8F2C5; // Stony Brook Cream

// Typography
$font-family-sans-serif: 'Barlow', 'Barlow Semi Condensed', sans-serif;
$font-family-serif: 'Bitter', serif;
$headings-font-family: 'Alumni Sans', sans-serif;

// Import Bootstrap and its default variables
@import 'bootstrap/scss/bootstrap';

// Additional custom styles
.display-heading {
  font-family: 'Alumni Sans', sans-serif;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.sbu-gradient {
  background: linear-gradient(45deg, #990000, #D52027);
}
```

---

## Component Examples

### Example Navigation Bar

```html
<nav class="navbar navbar-expand-lg navbar-dark" style="background-color: #990000;">
  <div class="container">
    <a class="navbar-brand" href="/">
      <img src="/path-to-sbu-logo.png" alt="Stony Brook University" height="40">
    </a>
    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent">
      <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="navbarSupportedContent">
      <ul class="navbar-nav ms-auto mb-2 mb-lg-0">
        <li class="nav-item">
          <a class="nav-link active" href="/">Home</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" href="/about">About</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" href="/services">Services</a>
        </li>
        <li class="nav-item">
          <a class="nav-link" href="/contact">Contact</a>
        </li>
      </ul>
    </div>
  </div>
</nav>
```

### Example Page Header

```html
<header class="py-5" style="background-color: #F8F2C5;">
  <div class="container">
    <h1 class="display-4 text-uppercase fw-bold" style="font-family: 'Alumni Sans', sans-serif; color: #990000;">
      DARE TO BE EXTRAORDINARY
    </h1>
    <p class="lead" style="font-family: 'Bitter', serif; color: #4B4B4B;">
      Welcome to the Stony Brook University application portal
    </p>
  </div>
</header>
```

### Example Button Styles

```html
<!-- Primary Button -->
<button class="btn btn-lg" style="background-color: #990000; color: white; font-family: 'Barlow Semi Condensed', sans-serif; font-weight: 600;">
  APPLY NOW
</button>

<!-- Secondary Button -->
<button class="btn btn-outline-dark" style="border-color: #990000; color: #990000; font-family: 'Barlow Condensed', sans-serif; font-weight: 500;">
  Learn More
</button>

<!-- Tertiary Button -->
<button class="btn" style="background-color: #1791AD; color: white; font-family: 'Barlow', sans-serif; font-weight: 500;">
  Contact Us
</button>
```

### Example Card Component

```html
<div class="card border-0 shadow-sm">
  <div class="card-header text-white" style="background-color: #990000; font-family: 'Alumni Sans', sans-serif; font-weight: 600; letter-spacing: 0.05em;">
    PROGRAM HIGHLIGHT
  </div>
  <div class="card-body">
    <h5 class="card-title" style="font-family: 'Bitter', serif; color: #4B4B4B; font-weight: 600;">
      Computer Science
    </h5>
    <p class="card-text" style="font-family: 'Barlow', sans-serif; color: #4B4B4B;">
      Our Computer Science program prepares students for careers in technology and innovation.
    </p>
    <a href="#" class="btn" style="background-color: #990000; color: white; font-family: 'Barlow Semi Condensed', sans-serif;">
      Learn More
    </a>
  </div>
</div>
```

### Example Footer

```html
<footer class="py-4 text-white" style="background-color: #000000;">
  <div class="container">
    <div class="row">
      <div class="col-md-4">
        <h5 style="font-family: 'Alumni Sans', sans-serif; letter-spacing: 0.05em;">STONY BROOK UNIVERSITY</h5>
        <p style="font-family: 'Barlow', sans-serif; font-size: 0.9rem;">
          100 Nicolls Road<br>
          Stony Brook, NY 11794
        </p>
      </div>
      <div class="col-md-4">
        <h5 style="font-family: 'Alumni Sans', sans-serif; letter-spacing: 0.05em;">LINKS</h5>
        <ul class="list-unstyled" style="font-family: 'Barlow', sans-serif; font-size: 0.9rem;">
          <li><a href="#" class="text-white text-decoration-none">Privacy Policy</a></li>
          <li><a href="#" class="text-white text-decoration-none">Accessibility</a></li>
          <li><a href="#" class="text-white text-decoration-none">Employment</a></li>
          <li><a href="#" class="text-white text-decoration-none">Emergency Info</a></li>
        </ul>
      </div>
      <div class="col-md-4">
        <h5 style="font-family: 'Alumni Sans', sans-serif; letter-spacing: 0.05em;">CONNECT</h5>
        <ul class="list-unstyled d-flex" style="font-family: 'Barlow', sans-serif; font-size: 0.9rem;">
          <li class="me-3"><a href="#" class="text-white text-decoration-none">Facebook</a></li>
          <li class="me-3"><a href="#" class="text-white text-decoration-none">Twitter</a></li>
          <li class="me-3"><a href="#" class="text-white text-decoration-none">Instagram</a></li>
          <li><a href="#" class="text-white text-decoration-none">LinkedIn</a></li>
        </ul>
      </div>
    </div>
    <div class="row mt-4">
      <div class="col-12 text-center" style="font-family: 'Barlow', sans-serif; font-size: 0.8rem;">
        <p>© 2025 Stony Brook University. All rights reserved.</p>
      </div>
    </div>
  </div>
</footer>
```