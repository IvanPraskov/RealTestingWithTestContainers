# Credit Scoring System

This repository contains a demonstration of a **Credit Scoring System** built with a layered approach and later refactored to a vertical slice architecture. It includes two projects:  
- **CreditScoringSystem**: The main application for credit scoring.  
- **EmploymentHistoryAPI**: A supporting API to retrieve employment history data.  

A **PostgreSQL** database is used to persist data.  

---

## Repository Structure

The repository is divided into several parts to demonstrate the evolution of the architecture:

### **Part 1: Initial Layered Approach**
- **Branch**: `part-1-initial-layered-approach`
- Implements the system using a layered architecture with controllers, services, and repositories.

### **Part 2: Vertical Sliced Approach**
- **Branch**: `part-2-vertical-sliced-approach`
- Refactors the system into vertical slices using API endpoints.  
- **Note**: Unit tests fail after this refactor because they were tightly coupled to the previous architecture. Integration tests pass.

### **Part 3: Fix Failing Unit Tests**
- **Branch**: Not yet implemented (`TODO`).  
- Intended to fix the failing unit tests after refactoring and implement the more complex system requirements.  

### **Simplified Versions for Presentation**
- **`part-1-initial-layered-approach-simplified`**: Simplified version of Part 1.  
- **`part-2-vertical-sliced-approach-simplified`**: Simplified version of Part 2.  
- **`part-3-fix-failing-unit-tests-simplified`**: Fixed unit tests and additional refactor, designed for presentation.  

### **Main Branch**
- Contains `part-1-initial-layered-approach` and `part-2-vertical-sliced-approach` merged together.  
- The solution **does not build** as `part-3` has not been implemented.

---

## Demo Requirements

The Credit Scoring System applies the following rules:

### **Credit Scoring Rules**
1. **Maximum Score**: 100.  
2. **Minimum Approval Score**: 50.  
3. **Manual Approval**: Required for scores between 50 and 60.  
4. **Approval Thresholds**:
   - **Score 80+**: Maximum credit = Net Monthly Income (NMI) × 20.  
   - **Score 70–79**: NMI × 10.  
   - **Score 60–69**: NMI × 5.  
   - **Score 50–59**: NMI × 3.  
   - **Score <50**: Rejected (0 credit).  

### **Penalties**:
- **Credit History**:  
  - No history + under 25 years old: -5.  
  - 1–2 missed payments: -10.  
  - 3+ missed payments: -30.  

- **Debt-to-Income Ratio**:  
  - 30% and below: No penalty.  
  - 30–50%: -10.  
  - 50–60%: -20.  
  - 60–70%: -30.  
  - Above 70%: -40.  

### **Bonuses**:
- **Employment Stability**:  
  - **Full-time**:
      - 3+ years: +10
      - 1–3 years: +5
      - less than 1 year: 0
  - **Part-time**:
      - 2+ years: +5
      - less than 2 years: 0
  - **Self-employed**:
      - 5+ years: +15
      - 2–5 years: +10
      - less than 2 years: 0 

---

## Key Features

- **Credit Scoring System**: Evaluates credit requests based on income, debt, and credit history.  
- **Employment History API**: Simulates an external service for fetching employment data.  
- **Integration Testing with Testcontainers**: Highlights how to avoid heavy mocking and create meaningful tests.  

---

## Next Steps

### **Part 3 - Fix Failing Unit Tests**
This branch is a **TODO**. Contributions are welcome to:
1. Fix the failing unit tests caused by tightly coupled mocks.  
2. Ensure compatibility with the more complex requirements.  

### **TODOs**
Ideas for new features to extend the functionality and practice adding such tests, extending the application functionality
- [ ] Add tests to cover all the uncovered cases, based on the provided requirements
- [ ] Use EF instead of Dapper
- [ ] Add endpoint to return only credit requests for Manual approval (score 50-60)
- [ ] Add endpoint to manually approve/reject a credit request
- [ ] On approval return multiple credit offers with different interest rates and periods
- [ ] Add eventing to notify other service/s once credit request is approved and try to test this

---

## Contributing

Feel free to fork the repository and contribute improvements, particularly for Part 3. For major changes, please open an issue first to discuss your ideas.  

## Running the Application

The repository includes a `docker-compose.yaml` file for running all services together in containers.  

### **Setup**
1. Install Docker and Docker Compose.
2. Clone the repository:  
   ```bash
   git clone https://github.com/IvanPraskov/RealTestingWithTestContainers.git
   cd RealTestingWithTestContainers
3. docker-compose up --build to start all services, docker-compose up --build credit-scoring-app to re-build only the credit scoring app
