pipeline {
    agent any
    
    environment {
        PROJECT_NAME = 'ProNet.Api'
        SOLUTION_FILE = 'Api.csproj'
        PUBLISH_DIR = 'publish'
        GITHUB_REPO = 'https://github.com/shrifzain/ApiNet.git'
        S3_BUCKET = 'pronet-artifacts'
        BLUE_SERVER = '54.152.102.169'  // Replace with Terraform output (e.g., 13.219.93.102)
        GREEN_SERVER = '54.158.110.100'  // Replace with Terraform output
        SSH_KEY_ID = 'sheraa-ssh-key'
        AWS_KEY_ID = 'awss'
        ACTIVE_ENV = 'blue'  // Start with blue as active
    }
    
    stages {
        stage('Get Code') {
            steps {
                echo 'Getting code from GitHub'
                git url: "${GITHUB_REPO}", branch: 'master', credentialsId: '01143717'
                sh 'ls -la'  // List root directory
                sh 'find . -type f'  // List all files in the repo to see full structure
                sh 'find . -name "*.csproj"'  // Find exact .csproj location
            }
        }
        
        stage('Build') {
            steps {
                echo 'Building the app'
                sh "dotnet publish ${SOLUTION_FILE} -c Release -o ${PUBLISH_DIR}"
            }
        }
        
        stage('Save to S3') {
            steps {
                echo 'Saving app to S3'
                withAWS(credentials: "${AWS_KEY_ID}") {
                    sh "aws s3 cp ${PUBLISH_DIR} s3://${S3_BUCKET}/${PROJECT_NAME}/ --recursive"
                }
            }
        }
        
stage('Deploy to Inactive Env') {
    steps {
        script {
            def inactiveEnv = env.ACTIVE_ENV == 'blue' ? 'green' : 'blue'
            def targetServer = inactiveEnv == 'blue' ? env.BLUE_SERVER : env.GREEN_SERVER
            
            echo "Deploying to ${inactiveEnv} (${targetServer})"
            sh """
                scp -i \$SSH_KEY -o StrictHostKeyChecking=no ${PUBLISH_DIR}/* ubuntu@${targetServer}:/home/ubuntu/app/
                ssh -i \$SSH_KEY -o StrictHostKeyChecking=no ubuntu@${targetServer} '
                    sudo systemctl daemon-reload
                    sudo systemctl enable pronet-api.service
                    sudo systemctl restart pronet-api.service
                    sudo systemctl status pronet-api.service --no-pager
                '
            """
            
            // Simple health check
            sh "curl --fail http://${targetServer}/ || exit 1"
        }
    }
}
    
    post {
        always {
            echo 'Cleaning up'
            cleanWs()
        }
    }
}
