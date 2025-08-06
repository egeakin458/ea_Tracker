describe('E2E Smoke: Investigators Dashboard', () => {
  beforeEach(() => {
    cy.intercept('GET', '/api/investigations', { fixture: 'investigators.json' });
    cy.visit('/');
  });

  it('displays the Investigators heading and rows', () => {
    cy.contains('Investigators');
    cy.get('table tbody tr').should('have.length', 2);
    cy.get('table').within(() => {
      cy.contains('Inv1');
      cy.contains('Inv2');
    });
  });
});
