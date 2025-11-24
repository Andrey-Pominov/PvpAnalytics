# PvpAnalytics

**Author:** Rmpriest

**Discord:** consonante_priest

PvP combat analytics platform for World of Warcraft combat logs. A microservices-based solution that parses arena matches, stores player data, and provides analytics through REST APIs.

## Project Overview

PvpAnalytics is a microservices platform that processes World of Warcraft combat log files to extract and analyze PvP arena match data. The platform consists of multiple services working together:

- **AuthService**: Handles user authentication and authorization
- **PvpAnalytics Service**: Processes combat logs and provides analytics APIs
- **PaymentService**: Handles payment transactions and payment management
- **UI**: React-based frontend dashboard