Feature: Dashboard Functionality
    As an authenticated user
    I want to view my dashboard
    So that I can see an overview of my work and projects

Background:
    Given the application is running

@dashboard @smoke
Scenario: Admin user views dashboard with statistics
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    When I navigate to the dashboard
    Then I should see the dashboard page
    And I should see project statistics
    And I should see sprint statistics
    And I should see task statistics

@dashboard
Scenario: Manager views dashboard
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    When I navigate to the dashboard
    Then I should see the dashboard page
    And I should see a welcome message with my name

@dashboard
Scenario: Developer views dashboard
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    When I navigate to the dashboard
    Then I should see the dashboard page
    And I should see my assigned tasks

@dashboard @navigation
Scenario: Navigate from dashboard to projects
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And I am on the dashboard
    When I click on the projects link
    Then I should be redirected to the projects page

@dashboard @navigation
Scenario: Navigate from dashboard to sprints
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And I am on the dashboard
    When I click on the sprints link
    Then I should be redirected to the sprints page

@dashboard @navigation
Scenario: Navigate from dashboard to weather
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And I am on the dashboard
    When I click on the weather link
    Then I should be redirected to the weather page

@dashboard
Scenario: Dashboard shows recent activity
    Given I am logged in as "venkateshboyapati96@gmail.com" with password "Govinda@1117"
    And some activities have occurred
    When I navigate to the dashboard
    Then I should see recent activity items

@dashboard @authorization
Scenario: Unauthenticated user cannot access dashboard
    Given I am not logged in
    When I attempt to navigate to the dashboard
    Then I should be redirected to the login page
